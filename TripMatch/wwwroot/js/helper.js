//放通用的
function getThemeColors() {
    const rootStyles = getComputedStyle(document.documentElement);
    return {
        error: rootStyles.getPropertyValue('--color_Contrast').trim(),
        success: rootStyles.getPropertyValue('--color_Green').trim()

    };
}
function getHintSelector(fieldId) {
    switch (fieldId) {
        case "email": return "#emailHint";
        case "password": return "#pwdHint";
        case "confirmPassword": return "#confirmPwdHint";
        default: return "#systemMessage";
    }
}
function ensureHintElement(selector, fieldId) {
    if ($(selector).length === 0) {
        // 嘗試在對應 input 元素後建立
        var input = $("#" + fieldId);
        if (input.length) {
            var $hint = $('<div>')
                .attr('id', selector.replace('#', ''))
                .addClass('inputHint');
            input.after($hint);
            return $hint;
        }
 
        var $fallback = $('<div>')
            .attr('id', selector.replace('#', ''))
            .addClass('inputHint');
        $('body').append($fallback);
        return null;
    }
    return $(selector);
}
function setFieldHint(fieldId, message, status) {
    try {
        var sel = getHintSelector(fieldId);
        var $el = ensureHintElement(sel, fieldId);

        // 確保元素存在
        if (!$el || $el.length === 0) return;

        // 清除舊樣式
        $el.removeClass('input_success input_error d-none');

        if (!message) {
            // 若無訊息則隱藏
            $el.text('').addClass('d-none');
            return;
        }

        // 顯示文字
        $el.text(message);

        // 根據狀態套用樣式
        var colors = getThemeColors();

        if (status === 'success') {
            $el.addClass('input_success');
            $el.css('color', colors.success || '#0a0');
        } else if (status === 'error') {
            $el.addClass('input_error');
            $el.css('color', colors.error || '#c00');
        } else {
            $el.css('color', '');
        }
    } catch (ex) {
        console.error("setFieldHint error:", ex);
    }
}

// 將函式暴露到全域，login.js / signup.js 可直接呼叫
window.setFieldHint = setFieldHint;
