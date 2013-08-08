function RegisterExternalViewModel(app, dataModel) {
    var self = this;

    // data-bind text
    self.externalAccessToken = ko.observable(null);
    self.loginProvider = ko.observable();

    // data-bind value
    self.userName = ko.observable(null);
    self.errors = ko.observableArray();

    // data-bind enable
    self.registering = ko.observable(false);

    // data-bind click
    self.registerClick = function () {
        self.errors.removeAll();
        self.registering(true);
        dataModel.registerExternal({
            userName: self.userName(),
            externalAccessToken: self.externalAccessToken()
        }).done(function (data) {
            self.registering(false);
            if (data.userName && data.access_token)
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
}
