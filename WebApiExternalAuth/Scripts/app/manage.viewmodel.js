function ManageViewModel(app, dataModel) {
    var self = this;

    var startedLoad = false;

    // data-bind foreach
    self.logins = ko.observableArray();
    self.externalLoginProviders = ko.observableArray();
    self.errors = ko.observableArray();

    // child coordination
    self.userName = ko.observable();
    self.localLoginProvider = ko.observable();
    self.hasLocalPassword = ko.computed(function () {
        var logins = self.logins();
        for (var i = 0; i < logins.length; i++)
            if (logins[i].loginProvider() === self.localLoginProvider())
                return true;
        return false;
    });

    // data-bind visible
    self.loading = ko.observable(true);
    self.externalLoginVisible = ko.computed(function () {
        return self.externalLoginProviders().length > 0;
    });

    // data-bind if
    self.showRemoveButton = ko.computed(function () {
        return self.logins().length > 1;
    });

    // data-bind with
    self.changePassword = ko.computed(function () {
        if (!self.hasLocalPassword())
            return null;

        return new ChangePasswordViewModel(app, self, self.userName(), dataModel);
    });
    self.setPassword = ko.computed(function () {
        if (self.hasLocalPassword())
            return null;

        return new SetPasswordViewModel(app, self, dataModel);
    });

    // data-bind value
    self.errors = ko.observableArray();

    // data-bind text
    self.message = ko.observable();

    // data-bind click
    self.todo = function () {
        app.navigateToTodo();
    };

    // parent coordination
    self.load = function () { // load user management info
        if (!startedLoad) {
            startedLoad = true;
            dataModel.getManageInfo(dataModel.returnUrl, true /* generateState */)
                .done(function (data) {
                    self.loading(false);
                    if (typeof (data.localLoginProvider) !== "undefined" &&
                        typeof (data.userName) !== "undefined" &&
                        typeof (data.logins) !== "undefined" &&
                        typeof (data.externalLoginProviders) !== "undefined") {
                        self.userName(data.userName);
                        self.localLoginProvider(data.localLoginProvider);
                        for (var i = 0; i < data.logins.length; i++)
                            self.logins.push(new RemoveLoginViewModel(data.logins[i], self, dataModel));
                        for (var i = 0; i < data.externalLoginProviders.length; i++)
                            self.externalLoginProviders.push(new AddExternalLoginProviderViewModel(app, data.externalLoginProviders[i]));
                    }
                    else
                        app.errors.push("Error retrieving user information.");
                }).failJSON(function (data) {
                    self.loading(false);
                    var errors = dataModel.toErrorsArray(data);
                    if (errors)
                        app.errors(errors);
                    else
                        app.errors.push("Error retrieving user information.");
                });
        }
    }

    self.addExternalLogin = function (externalAccessToken, externalError) {
        if (externalError != null || externalAccessToken == null) {
            self.errors.push("Failed to associated external login.");
            self.load();
        }
        else
            dataModel.addExternalLogin({
                externalAccessToken: externalAccessToken
            }).done(function (data) {
                self.load();
            }).failJSON(function (data) {
                var errors = dataModel.toErrorsArray(data);
                if (errors)
                    self.errors(errors);
                else
                    self.errors.push("An unknown error occurred.");
                self.load();
            });
    };
}

function AddExternalLoginProviderViewModel(app, data) {
    var self = this;

    // data-bind text
    self.name = ko.observable(data.name);

    // data-bind click
    self.loginClick = function () {
        sessionStorage["state"] = data.state;
        sessionStorage["associatingExternalLogin"] = true;
        // IE doesn't reliably persist sessionStorage when navigating to another URL. Move sessionStorage temporarily
        // to localStorage to work around this problem.
        app.archiveSessionStorageToLocalStorage();
        window.location = data.url;
    };
}

function ChangePasswordViewModel(app, parent, name, dataModel) {
    var self = this;

    // data-bind foreach
    self.errors = ko.observableArray();

    // data-bind text
    self.name = ko.observable(name);

    // data-bind value
    self.oldPassword = ko.observable("");
    self.newPassword = ko.observable("");
    self.confirmPassword = ko.observable("");

    // data-bind enable
    self.changing = ko.observable(false);

    function reset() {
        self.errors.removeAll();
        self.oldPassword(null);
        self.newPassword(null);
        self.confirmPassword(null);
        self.changing(false);
    }

    // data-bind click
    self.changeClick = function () {
        self.errors.removeAll();
        self.changing(true);
        dataModel.changePassword({
            oldPassword: self.oldPassword(),
            newPassword: self.newPassword(),
            confirmPassword: self.confirmPassword()
        }).done(function (data) {
            self.changing(false);
            reset();
            parent.message("Your password has been changed.");
        }).failJSON(function (data) {
            self.changing(false);
            var errors = dataModel.toErrorsArray(data);
            if (errors)
                self.errors(errors);
            else
                self.errors.push("An unknown error occurred.");
        });
    };
}

function RemoveLoginViewModel(data, parent, dataModel) {
    var self = this;

    // data-bind text
    self.loginProvider = ko.observable(data.loginProvider);
    self.providerKey = ko.observable(data.providerKey);

    // data-bind enable
    self.removing = ko.observable(false);

    // data-bind click
    self.removeClick = function () {
        parent.errors.removeAll();
        self.removing(true);
        dataModel.removeLogin({
            loginProvider: self.loginProvider(),
            providerKey: self.providerKey()
        }).done(function (data) {
            self.removing(false);
            parent.logins.remove(self);
            parent.message("The login was removed.");
        }).failJSON(function (data) {
            self.removing(false);
            var errors = dataModel.toErrorsArray(data);
            if (errors)
                parent.errors(errors);
            else
                parent.errors.push("An unknown error occurred.");
        });
    };
}

function SetPasswordViewModel(app, parent, dataModel) {
    var self = this;

    // data-bind foreach
    self.errors = ko.observableArray();

    // data-bind value
    self.newPassword = ko.observable("");
    self.confirmPassword = ko.observable("");

    // data-bind visible
    self.setting = ko.observable(false);

    // data-bind click
    self.setClick = function () {
        self.errors.removeAll();
        self.setting(true);
        dataModel.setPassword({
            newPassword: self.newPassword(),
            confirmPassword: self.confirmPassword()
        }).done(function (data) {
            self.setting(false);
            parent.logins.push(new RemoveLoginViewModel({ loginProvider: parent.localLoginProvider(), providerKey: parent.userName() }, parent, dataModel));
            parent.message("Your password has been set.");
        }).failJSON(function (data) {
            self.setting(false);
            var errors = dataModel.toErrorsArray(data);
            if (errors)
                self.errors(errors);
            else
                self.errors.push("An unknown error occurred.");
        });
    };
}
