function UserInfoViewModel(app, name, dataModel) {
    var self = this;

    // data-bind text
    self.name = ko.observable(name);

    // data-bind click
    self.logOff = function () {
        app.navigateToLoggedOff();
    };

    self.manage = function () {
        app.navigateToManage();
    };
}
