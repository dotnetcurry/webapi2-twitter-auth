function AppDataModel() {
    var self = this;

    function getSecurityHeaders() {
        var accessToken = sessionStorage["accessToken"] || localStorage["accessToken"];

        if (accessToken) {
            return { "Authorization": "Bearer " + accessToken };
        }

        return {};
    }

    self.clearAccessToken = function () {
        localStorage.removeItem("accessToken");
        sessionStorage.removeItem("accessToken");
    };
    self.setAccessToken = function (accessToken, persistent) {
        if (persistent)
            localStorage["accessToken"] = accessToken;
        else
            sessionStorage["accessToken"] = accessToken;
    };

    self.toErrorsArray = function (data) {
        if (!data || !data.message)
            return null;

        var errors = new Array();

        if (data.modelState)
            for (var key in data.modelState) {
                var items = data.modelState[key];

                if (items.length)
                    for (var i = 0; i < items.length; i++)
                        errors.push(items[i]);
            }

        if (errors.length === 0)
            errors.push(data.message);

        return errors;
    };

    // ajax helper
    function ajaxRequest(type, url, data, dataType) {
        var options = {
            dataType: dataType || "json",
            contentType: "application/json",
            cache: false,
            type: type,
            data: data ? data.toJson() : null,
            headers: getSecurityHeaders()
        };
        return $.ajax(url, options);
    }

    // routes
    function externalLoginCompleteUrl(accessToken) { return "/api/Account/ExternalLoginComplete?access_token=" + (accessToken || ""); }
    function externalLoginsUrl(returnUrl, generateState) { return "/api/Account/ExternalLogins?returnUrl=" + (encodeURIComponent(returnUrl)) + "&generateState=" + (generateState ? "true" : "false"); }
    function manageInfoUrl(returnUrl, generateState) { return "/api/Account/ManageInfo?returnUrl=" + (encodeURIComponent(returnUrl)) + "&generateState=" + (generateState ? "true" : "false"); }
    function todoItemUrl(id) { return "/api/Todo/" + (id || ""); }
    function todoListUrl(id) { return "/api/TodoList/" + (id || ""); }
    var addExternalLoginUrl = "/api/Account/AddExternalLogin";
    var changePasswordUrl = "/api/Account/changePassword";
    var loginUrl = "/api/Account/Login";
    var registerUrl = "/api/Account/Register";
    var registerExternalUrl = "/api/Account/RegisterExternal";
    var removeLoginUrl = "/api/Account/RemoveLogin";
    var setPasswordUrl = "/api/Account/setPassword";
    var siteUrl = "/";
    var userInfoUrl = "/api/Account/UserInfo";

    self.returnUrl = siteUrl;

    // data access
    self.addExternalLogin = function (data) {
        return $.ajax(addExternalLoginUrl, {
            type: "POST",
            data: data,
            headers: getSecurityHeaders()
        });
    };
    self.changePassword = function (data) {
        return $.ajax(changePasswordUrl, {
            type: "POST",
            data: data,
            headers: getSecurityHeaders()
        });
    };
    self.deleteTodoItem = function (todoItem) {
        return ajaxRequest("DELETE", todoItemUrl(todoItem.todoItemId));
    };
    self.deleteTodoList = function (todoList) {
        return ajaxRequest("DELETE", todoListUrl(todoList.todoListId));
    };
    self.externalLoginComplete = function (accessToken) {
        return $.ajax(externalLoginCompleteUrl(accessToken));
    };
    self.getExternalLogins = function (returnUrl, generateState) {
        return ajaxRequest("GET", externalLoginsUrl(returnUrl, generateState));
    };
    self.getManageInfo = function (returnUrl, generateState) {
        return ajaxRequest("GET", manageInfoUrl(returnUrl, generateState));
    };
    self.getTodoLists = function () {
        return ajaxRequest("GET", todoListUrl());
    };
    self.getUserInfo = function () {
        return ajaxRequest("GET", userInfoUrl);
    };
    self.login = function (data) {
        return $.ajax(loginUrl, {
            type: "POST",
            data: data
        });
    };
    self.register = function (data) {
        return $.ajax(registerUrl, {
            type: "POST",
            data: data
        });
    };
    self.registerExternal = function (data) {
        return $.ajax(registerExternalUrl, {
            type: "POST",
            data: data
        });
    };
    self.removeLogin = function (data) {
        return $.ajax(removeLoginUrl, {
            type: "POST",
            data: data,
            headers: getSecurityHeaders()
        });
    };
    self.saveChangedTodoItem = function (todoItem) {
        return ajaxRequest("PUT", todoItemUrl(todoItem.todoItemId), todoItem, "text");
    };
    self.saveChangedTodoList = function (todoList) {
        return ajaxRequest("PUT", todoListUrl(todoList.todoListId), todoList, "text");
    };
    self.saveNewTodoItem = function (todoItem) {
        return ajaxRequest("POST", todoItemUrl(), todoItem);
    };
    self.saveNewTodoList = function (todoList) {
        return ajaxRequest("POST", todoListUrl(), todoList);
    };
    self.setPassword = function (data) {
        return $.ajax(setPasswordUrl, {
            type: "POST",
            data: data,
            headers: getSecurityHeaders()
        });
    };
}
