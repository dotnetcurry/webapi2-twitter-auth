function AppViewModel(dataModel) {
    var self = this;

    var Views = {
        Loading: 0,
        Todo: 1,
        Login: 2,
        Register: 3,
        RegisterExternal: 4,
        Manage: 5
    };

    var view = ko.observable(Views.Loading);

    // child coordination
    self.errors = ko.observableArray();

    self.archiveSessionStorageToLocalStorage = function () {
        var backup = {};
        for (var i = 0; i < sessionStorage.length; i++)
            backup[sessionStorage.key(i)] = sessionStorage[sessionStorage.key(i)];
        localStorage["sessionStorageBackup"] = JSON.stringify(backup);
        sessionStorage.clear();
    };

    self.restoreSessionStorageFromLocalStorage = function () {
        var backupText = localStorage["sessionStorageBackup"];

        if (backupText) {
            var backup = JSON.parse(backupText);

            for (var key in backup) {
                sessionStorage[key] = backup[key];
            }

            localStorage.removeItem("sessionStorageBackup");
        }
    };

    self.navigateToLoggedIn = function (userName, accessToken, persistent) {
        self.errors.removeAll();

        if (accessToken)
            dataModel.setAccessToken(accessToken, persistent)

        self.user(new UserInfoViewModel(self, userName, dataModel));
        self.navigateToTodo();
    };
    self.navigateToTodo = function () {
        self.errors.removeAll();
        view(Views.Todo);
    };
    self.navigateToLoggedOff = function () {
        self.errors.removeAll();
        dataModel.clearAccessToken();
        self.navigateToLogin();
    };
    self.navigateToLogin = function () {
        self.errors.removeAll();
        self.user(null);
        view(Views.Login);
    };
    self.navigateToRegister = function () {
        self.errors.removeAll();
        view(Views.Register);
    };
    self.navigateToRegisterExternal = function (userName, loginProvider, externalAccessToken) {
        self.errors.removeAll();
        view(Views.RegisterExternal);
        self.registerExternal().userName(userName);
        self.registerExternal().loginProvider(loginProvider);
        self.registerExternal().externalAccessToken(externalAccessToken);
    };
    self.navigateToManage = function (externalAccessToken, externalError) {
        self.errors.removeAll();
        view(Views.Manage);

        if (typeof (externalAccessToken) !== "undefined" || typeof (externalError) !== "undefined")
            self.manage().addExternalLogin(externalAccessToken, externalError);
        else
            self.manage().load();
    };

    // data-bind with
    self.user = ko.observable(null);

    self.loading = ko.computed(function () {
        return view() === Views.Loading;
    });
    self.todo = ko.computed(function () {
        if (view() !== Views.Todo)
            return null;

        return new TodoViewModel(self, dataModel);
    });
    self.login = ko.computed(function () {
        if (view() !== Views.Login)
            return null;

        return new LoginViewModel(self, dataModel);
    });
    self.register = ko.computed(function () {
        if (view() !== Views.Register)
            return null;

        return new RegisterViewModel(self, dataModel);
    });
    self.registerExternal = ko.computed(function () {
        if (view() !== Views.RegisterExternal)
            return null;

        return new RegisterExternalViewModel(self, dataModel);
    });
    self.manage = ko.computed(function () {
        if (view() !== Views.Manage)
            return null;

        return new ManageViewModel(self, dataModel);
    });

    function cleanUpLocation() {
        window.location.hash = "";

        if (typeof (history.pushState) !== "undefined") {
            history.pushState("", document.title, location.pathname);
        }
    }

    function parseQueryString(queryString) {
        var data = {};

        if (queryString == null)
            return data;

        var pairs = queryString.split("&");
        for (var i = 0; i < pairs.length; i++) {
            var pair = pairs[i];
            var separatorIndex = pair.indexOf("=");

            var escapedKey;
            var escapedValue;

            if (separatorIndex == -1) {
                escapedKey = pair;
                escapedValue = null;
            }
            else {
                escapedKey = pair.substr(0, separatorIndex);
                escapedValue = pair.substr(separatorIndex + 1);
            }

            var key = decodeURIComponent(escapedKey);
            var value = decodeURIComponent(escapedValue);

            data[key] = value;
        }

        return data;
    }

    function verifyStateMatch(fragment) {
        if (typeof (fragment.access_token) !== "undefined") {
            var state = sessionStorage["state"];
            sessionStorage.removeItem("state");

            if (state == null || fragment.state != state)
                fragment.error = "invalid_state";
        }
    }

    var fragment;
    if (window.location.hash.indexOf("#") == 0)
        fragment = parseQueryString(window.location.hash.substr(1));
    else
        fragment = {};

    self.restoreSessionStorageFromLocalStorage();

    verifyStateMatch(fragment);

    if (sessionStorage["associatingExternalLogin"]) {
        sessionStorage.removeItem("associatingExternalLogin");

        var externalAccessToken;
        var externalError;
        if (typeof (fragment.error) !== "undefined") {
            externalAccessToken = null;
            externalError = fragment.error;
            cleanUpLocation();
        }
        else if (typeof (fragment.access_token) !== "undefined") {
            externalAccessToken = fragment.access_token;
            externalError = null;
            cleanUpLocation();
        }
        else {
            externalAccessToken = null;
            externalError = null;
            cleanUpLocation();
        }

        dataModel.getUserInfo()
            .done(function (data) {
                if (data.userName) {
                    self.navigateToLoggedIn(data.userName);
                    self.navigateToManage(externalAccessToken, externalError);
                }
                else {
                    self.navigateToLogin();
                }
            })
            .fail(function () {
                self.navigateToLogin();
            });
    }
    else if (typeof (fragment.error) !== "undefined") {
        cleanUpLocation();
        self.navigateToLogin();
        self.errors.push("External login failed.");
    }
    else if (typeof (fragment.access_token) !== "undefined") {
        cleanUpLocation();
        dataModel.externalLoginComplete(fragment.access_token)
            .done(function (data) {
                if (data.userName && data.access_token)
                    self.navigateToLoggedIn(data.userName, data.access_token, false);
                else if (typeof (data.userName) !== "undefined" && typeof (data.loginProvider) !== "undefined")
                    self.navigateToRegisterExternal(data.userName, data.loginProvider, fragment.access_token);
                else
                    self.navigateToLogin();
            })
            .failJSON(function (data) {
                self.navigateToLogin();
                var errors = errorsToArray(data);
                if (errors)
                    self.errors(errors);
            });
    }
    else
        dataModel.getUserInfo()
            .done(function (data) {
                if (data.userName)
                    self.navigateToLoggedIn(data.userName);
                else
                    self.navigateToLogin();
            })
            .fail(function () {
                self.navigateToLogin();
            });
}

// activate knockout
ko.applyBindings(new AppViewModel(new AppDataModel()));
