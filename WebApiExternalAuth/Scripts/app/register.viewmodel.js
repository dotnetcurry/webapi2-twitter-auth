function RegisterViewModel(app, dataModel) {
    var self = this;

    // data-bind value
    self.userName = ko.observable("");
    self.password = ko.observable("");
    self.confirmPassword = ko.observable("");
    self.errors = ko.observableArray();

    // data-bind enable
    self.registering = ko.observable(false);

    // data-bind click
    self.registerClick = function () {
        self.errors.removeAll();
        self.registering(true);
        dataModel.register({
            userName: self.userName(),
            password: self.password(),
            confirmPassword: self.confirmPassword()
        }).done(function (data) {
            self.registering(false);
            if (data.errors)
                self.errors(data.errors);
            else if (data.userName && data.access_token)
                app.navigateToLoggedIn(data.userName, data.access_token, false);
            else
                self.errors.push("An unknown error occurred.");
        }).failJSON(function (data) {
            self.registering(false);
            var errors = dataModel.toErrorsArray(data);
            if (errors)
                self.errors(errors);
            else
                self.errors.push("An unknown error occurred.");
        });
    };

    self.loginClick = function () {
        app.navigateToLogin();
    };
}
