function LoginViewModel(app, dataModel) {
    var self = this;

    // implementation details
    var validationTriggered = ko.observable(false);

    // data-bind foreach
    self.externalLoginProviders = ko.observableArray();

    // data-bind value
    self.userName = ko.observable("").extend({
        required: {
            enabled: validationTriggered,
            message: "The User name field is required."
        }
    });
    self.password = ko.observable("").extend({
        required: {
            enabled: validationTriggered,
            message: "The Password field is required."
        }
    });
    self.rememberMe = ko.observable(false);
    self.errors = ko.observableArray();

    // data-bind visible
    self.loadingExternalLogin = ko.observable(true);
    self.externalLoginVisible = ko.computed(function () {
        return self.externalLoginProviders().length > 0;
    });

    // data-bind enable
    self.loggingIn = ko.observable(false);

    // data-bind click
    self.loginClick = function () {
        self.errors.removeAll();
        validationTriggered(true);
        if (self.userName.hasError() || self.password.hasError())
            return;
        self.loggingIn(true);

        dataModel.login({
            grant_type: "password",
            username: self.userName(),
            password: self.password()
        }).done(function (data) {
            self.loggingIn(false);
            if (data.userName && data.access_token)
                app.navigateToLoggedIn(data.userName, data.access_token, self.rememberMe());
            else
                self.errors.push("An unknown error occurred.");
        }).failJSON(function (data) {
            self.loggingIn(false);
            if (data && data.error_description)
                self.errors.push(data.error_description);
            else
                self.errors.push("An unknown error occurred.");
        });
    };

    self.registerClick = function () {
        app.navigateToRegister();
    };

    dataModel.getExternalLogins(dataModel.returnUrl, true /* generateState */)
        .done(function (data) {
            self.loadingExternalLogin(false);
            if (typeof (data) === "object")
                for (var i = 0; i < data.length; i++)
                    self.externalLoginProviders.push(new ExternalLoginProviderViewModel(app, data[i]));
            else
                self.errors.push("An unknown error occurred.");
        }).fail(function () {
            self.loadingExternalLogin(false);
            self.errors.push("An unknown error occurred.");
        });
}

function ExternalLoginProviderViewModel(app, data) {
    var self = this;

    // data-bind text
    self.name = ko.observable(data.name);

    // data-bind click
    self.loginClick = function () {
        sessionStorage["state"] = data.state;
        // IE doesn't reliably persist sessionStorage when navigating to another URL. Move sessionStorage temporarily
        // to localStorage to work around this problem.
        app.archiveSessionStorageToLocalStorage();
        window.location = data.url;
    };
}
