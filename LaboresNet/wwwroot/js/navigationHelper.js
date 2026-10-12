window.navigationHelper = {
    goBack: function () {
        if (window.history.length > 1) {
            window.history.back();
            return true;
        }
        return false;
    },
    canGoBack: function () {
        return window.history.length > 1;
    }
};
