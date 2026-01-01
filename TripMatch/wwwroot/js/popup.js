(function (window, $) {

    let popupOpen = false;

    function showPopup(options) {

   
        if (popupOpen) {
            return Promise.resolve();
        }

        popupOpen = true;

        return new Promise((resolve) => {

            const {
                title = "",
                message = "",
                type = "success",
                autoClose = false,
                seconds = 3
            } = options;

            const statusClass = type === "success" ? "popup_success" : "popup_error";

            const popupHtml = `
                <div class="popup_overlay"></div>
                <div class="reg_popup">
                    <span class="popup_title ${statusClass}">${title}</span>
                    <p class="titleH5 popH5">${message}</p>
                    ${autoClose ? `
                    <div class="popTime">
                        此視窗將於 <span id="popupSec">${seconds}</span> 秒後自動關閉
                    </div>` : ""}
                    <button class="btn_popup_close">確定</button>
                </div>
            `;

            $("body").append(popupHtml);

            let timer = null;

            if (autoClose) {
                let remaining = seconds;
                timer = setInterval(() => {
                    remaining--;
                    $("#popupSec").text(remaining);
                    if (remaining <= 0) closePopup();
                }, 1000);
            }

            $(".btn_popup_close, .popup_overlay").on("click", closePopup);

            function closePopup() {
                if (timer) clearInterval(timer);

                $(".popup_overlay, .reg_popup").fadeOut(300, function () {
                    $(this).remove();
                    popupOpen = false;   // ★ 解鎖
                    resolve();
                });
            }
        });
    }

    window.showPopup = showPopup;

})(window, jQuery);
