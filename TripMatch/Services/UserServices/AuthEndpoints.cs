using Lab1224_Identity.Models;
using Lab1224_Identity.Models.Settings;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Lab1224_Identity.Services
{
    public static class AuthEndpoints
    {
        // 擴充 IEndpointRouteBuilder，讓它可以在 app 變數上點出來
        public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/auth");

            //新使用者：
            //建帳號 -> 寄信 -> 點連結領 Cookie -> 填密碼。
            //Cookie 掉了的老客戶：
            //輸入 Email -> 系統發現沒密碼 -> 重新寄信 -> 點連結「補領」Cookie -> 回原頁填密碼。

            //測試用：自動產生假會員並登入
            group.MapPost("/test-generate-user", async (TestingService testingService,AuthService authService,HttpContext context,UserManager<ApplicationUser> userManager) =>
            {
                // 1. 呼叫服務直接取得 userId
                var (succeeded, userId, userName, error) = await testingService.CreateFakeUserAsync();

                if (!succeeded) return Results.BadRequest(new { message = error });

                // 2. 為了產生 Token，我們還是需要 User 物件
                var user = await userManager.FindByIdAsync(userId.ToString());
                if (user == null) return Results.NotFound();

                // 3. 產生 JWT 並寫入 Cookie
                var token = authService.GenerateJwtToken(user);
                authService.SetAuthCookie(context, token);

                // 4. 回傳給前端 (組員 B 測試時可以看到目前是哪位 ID)
                return Results.Ok(new
                {
                    message = "成功新增假會員並自動登入",
                    userId = userId, // 這就是您要的 userID (int)
                    userName = userName
                });
            });



            // 註冊
            group.MapPost("/register", async ([FromBody] Register model, UserManager<ApplicationUser> userManager, HttpContext context) =>
            {
                    if (!context.Request.Cookies.TryGetValue("PendingEmail", out var pendingEmail))
                    {
                        return Results.BadRequest(new { message = "驗證逾時，請重新驗證 Email" });
                    }
                    var user = await userManager.FindByEmailAsync(model.Email);
                    if (user == null || !user.EmailConfirmed)
                    {

                        return Results.BadRequest(new { message = "請先完成Email驗證" });
                    }

                    //防止重複註冊
                    var hasPassword = await userManager.HasPasswordAsync(user);
                    if (hasPassword)
                    {
                        return Results.Conflict(new { message = "該帳號已完成設定，請直接登入" });
                    }
                    //正式設定密碼
                    var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);
                    var result = await userManager.ResetPasswordAsync(user, resetToken, model.Password);
                    if (result.Succeeded)
                    {
                        //註冊成功後，刪除暫存的 PendingEmail Cookie
                        context.Response.Cookies.Delete("PendingEmail");

                        return Results.Ok(new { message = "帳戶設定成功！請登入" });
                    }
                    return Results.BadRequest(new { errors = result.Errors });
        
            });

            // 登入
            group.MapPost("/login", async ([FromBody] LoginModel model, SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager, AuthService authService, HttpContext context) =>
            {
                    var user = await userManager.FindByEmailAsync(model.Email);
                if(user == null) return Results.Unauthorized();
                var result = await signInManager.PasswordSignInAsync(user, model.Password, isPersistent: false, lockoutOnFailure: true);
             
                if (result.Succeeded)
                    {
                        // 產生 JWT
                        var token = authService.GenerateJwtToken(user);

                        authService.SetAuthCookie(context, token);
                        authService.SetPendingCookie(context, user.Email);

                        return Results.Ok(new { message = "登入成功" });
                    }

                if (result.IsLockedOut)
                {
                    return Results.Json(new { message = "帳號已被鎖定，請於 5 分鐘後再試。" }, statusCode: 423);
                }
                // 計算剩餘次數
                int accessFailedCount = await userManager.GetAccessFailedCountAsync(user);
                int remainingAttempts = 5 - accessFailedCount;

                return Results.BadRequest(new { message = $"帳號或密碼錯誤。剩餘嘗試次數：{remainingAttempts}" });
            });

            // 發送驗證信
            group.MapPost("/send-confirmation", async ([FromBody] string email, UserManager<ApplicationUser> userManager, IEmailSender<ApplicationUser> emailSender, AuthService authService, HttpContext context) =>
            {

                    var user = await userManager.FindByEmailAsync(email);
                    if (context?.Request == null)
                    {
                        return Results.BadRequest("無法取得請求資訊");
                    }
                    if (user != null)
                    {
                        var reCode = await userManager.GenerateEmailConfirmationTokenAsync(user);
                        var reUrl = $"{context.Request.Scheme}://{context.Request.Host}/api/auth/confirm-email?userId={user.Id}&code={System.Net.WebUtility.UrlEncode(reCode)}";
                        // 情況 A: 已經完全註冊好（有密碼） -> 叫他去登入
                        if (!string.IsNullOrEmpty(user.PasswordHash) && user.PasswordHash != "TempP@ss123")
                        {
                            return Results.Conflict(new { action = "redirect_login", message = "Email 已註冊，請直接登入。" });
                        }

                        // 情況 B: 已驗證信箱但還沒設密碼 (這就是你要的停留原頁)
                        if (user.EmailConfirmed)
                        {
                            authService.SetPendingCookie(context, user.Email); // 補發 Cookie 確保同步
                            return Results.Ok(new { verified = true, message = "此帳號已驗證成功，請直接設定密碼。" });
                        }
                    }
                    //情況C:完全沒有帳號的新使用者，先建立一個虛擬使用者
                    if (user == null)
                    {
                        user = new ApplicationUser { UserName = email, Email = email };
                        var createResult = await userManager.CreateAsync(user, "TempP@ss123");
                        if (!createResult.Succeeded) return Results.BadRequest("系統錯誤，請重新發送驗證信");
                    }



                    // 產生驗證 Token
                    var code = await userManager.GenerateEmailConfirmationTokenAsync(user);

                    var callbackUrl = authService.GenerateConfirmUrl(context, user.Id, code);

                    await emailSender.SendConfirmationLinkAsync(user, email, callbackUrl);

                    authService.SetPendingCookie(context, user.Email);

                    return Results.Ok(new { message = "驗證信已發送，請檢查信箱或垃圾郵件。" });

            });

            // 登出
            group.MapPost("/logout", async (SignInManager<ApplicationUser> signInManager, HttpContext context) =>
            {
                await signInManager.SignOutAsync();
                context.Response.Cookies.Delete("AuthToken");
                return Results.Ok(new { message = "已登出" });
            }).RequireAuthorization();


            group.MapGet("/check-db-status", async (HttpContext context,UserManager<ApplicationUser> userManager) =>
            {
                // 後端直接從 Cookie 拿 Email，前端無法偽造
                if (!context.Request.Cookies.TryGetValue("PendingEmail", out var email))
                    return Results.Ok(new { verified = false });

                var user = await userManager.FindByEmailAsync(email);

                if (user != null && user.EmailConfirmed)
                {
                    return Results.Ok(new { verified = true, email = email });
                }
                else if (user != null && user.EmailConfirmed && string.IsNullOrEmpty(user.PasswordHash))
                { return Results.Ok(new { verified = true, email = email }); }

                return Results.Ok(new { verified = false });
            });

            group.MapPost("/check-email-status", async ( [FromBody] string email,UserManager<ApplicationUser> userManager) =>
            {
                var user = await userManager.FindByEmailAsync(email);
                if (user != null && user.EmailConfirmed)
                {
                    return Results.Ok(new { verified = true });
                }
                return Results.Ok(new { verified = false });
            });


            group.MapGet("/confirm-email", async ([FromQuery] string userId,[FromQuery] string code,UserManager<ApplicationUser> userManager,AuthService authService,HttpContext context) =>
            {
                var user = await userManager.FindByIdAsync(userId);
                if (user == null) return Results.Redirect("/checkemail.html?status=error");

                var result = await userManager.ConfirmEmailAsync(user, code);

                //驗證成功,或是已經驗證過但需要重新給Cookie
                if (result.Succeeded || (user != null && user.EmailConfirmed))
                {
                    // ★ 補發或寫入 PendingEmail Cookie
                    authService.SetPendingCookie(context, user.Email);

                    // 驗證成功，導向美化頁面
                    return Results.Redirect("/checkemail.html?status=success");
                }

                return Results.Redirect("/checkemail.html?status=error");
            });

        }
    }
}