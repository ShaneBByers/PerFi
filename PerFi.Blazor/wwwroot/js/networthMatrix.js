window.perfiNetWorth = {
    scrollToRight: function (elementId) {
        var el = document.getElementById(elementId);
        if (el) {
            el.scrollLeft = el.scrollWidth;
        }
    }
};
