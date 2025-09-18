"use strict";
var __extends = (this && this.__extends) || (function () {
    var extendStatics = function (d, b) {
        extendStatics = Object.setPrototypeOf ||
            ({ __proto__: [] } instanceof Array && function (d, b) { d.__proto__ = b; }) ||
            function (d, b) { for (var p in b) if (Object.prototype.hasOwnProperty.call(b, p)) d[p] = b[p]; };
        return extendStatics(d, b);
    };
    return function (d, b) {
        if (typeof b !== "function" && b !== null)
            throw new TypeError("Class extends value " + String(b) + " is not a constructor or null");
        extendStatics(d, b);
        function __() { this.constructor = d; }
        d.prototype = b === null ? Object.create(b) : (__.prototype = b.prototype, new __());
    };
})();
//region Dark Mode
// noinspection JSUnusedGlobalSymbols
function setDarkMode(b) {
    localStorage && localStorage.setItem("dark", "" + b);
    document.documentElement.classList[b ? "add" : "remove"]("dark");
}
{
    var storageDark = localStorage && localStorage.getItem("dark");
    var wantsDark = storageDark == "true" || storageDark == "false" ? storageDark == "true" : window.matchMedia('(prefers-color-scheme: dark)').matches;
    document.documentElement.classList[wantsDark ? "add" : "remove"]("dark");
}
//@ts-ignore
// noinspection JSDeprecatedSymbols
var windowExternal = window["external"];
var ownExternal = windowExternal && windowExternal.Available ? windowExternal : null;
//region send&receive
var pending = [];
var ws;
send(null);
function send(data) {
    if (data != null)
        pending.push(typeof data == "string" ? data : JSON.stringify(data));
    if (ws && (ws == ownExternal || ws.readyState == WebSocket.OPEN)) {
        if (!pending.length)
            return;
        ws.send(pending.shift());
        setTimeout(send, 0);
    }
    else if (!ws || ws.readyState == WebSocket.CLOSING || ws.readyState == WebSocket.CLOSED) {
        if (ownExternal) {
            ws = ownExternal;
            ownExternal.Init(function (s) { return receive(JSON.parse(s)); });
            setTimeout(send, 0);
            return;
        }
        var url = "ws" + document.location.origin.substring(4);
        ws = new WebSocket(url);
        ws.onclose = function () {
            ws.close();
            setTimeout(send, 100);
        };
        ws.onerror = function () {
            ws.close();
            setTimeout(send, 100);
        };
        ws.onmessage = function (e) {
            receive(JSON.parse(e.data));
        };
        ws.onopen = function () {
            setTimeout(send, 0);
        };
    }
}
function receive(json) {
    if (Array.isArray(json)) {
        var value = json.pop();
        var obj = registered;
        for (var _i = 0, json_1 = json; _i < json_1.length; _i++) {
            var key = json_1[_i];
            obj = obj && obj[key];
        }
        if (!obj)
            json.push(value);
        else {
            //console.log("Received:",json,value);
            obj(value);
            return;
        }
    }
    else {
        //console.log("HotString:",json);
        updateHotString(json);
        return;
    }
    console.warn("Received unknown: ", json);
}
var registered = {};
function register(keys, func) {
    var obj = registered;
    var last = keys.pop();
    for (var _i = 0, keys_1 = keys; _i < keys_1.length; _i++) {
        var key = keys_1[_i];
        obj = (obj[key] || (obj[key] = {}));
    }
    obj[last] = func;
}
//endregion
//region Navigation
document.addEventListener("DOMContentLoaded", function initNavigation() {
    if (ownExternal) {
        var _loop_1 = function (a) {
            var href = a.getAttribute("href");
            a.onclick = function (e) {
                ownExternal.Hash = href;
                e.preventDefault();
                onHashChange();
            };
        };
        for (var _i = 0, _a = document.querySelectorAll("nav>a"); _i < _a.length; _i++) {
            var a = _a[_i];
            _loop_1(a);
        }
    }
    onHashChange();
});
window.onhashchange = onHashChange;
function onHashChange() {
    var hash = ownExternal ? ownExternal.Hash : document.location.hash;
    var defaultHash = "#windows";
    var found = hash == defaultHash;
    for (var _i = 0, _a = document.querySelectorAll("main>*"); _i < _a.length; _i++) {
        var section = _a[_i];
        if (("#" + section.id) == hash) {
            section.classList.add("active");
            found = true;
        }
        else
            section.classList.remove("active");
    }
    if (!found) {
        if (ownExternal)
            ownExternal.Hash = defaultHash;
        else
            document.location.hash = defaultHash;
        return onHashChange();
    }
    for (var _b = 0, _c = document.querySelectorAll("nav>a"); _b < _c.length; _b++) {
        var a = _c[_b];
        if (a.getAttribute("href") == hash)
            a.classList.add("active");
        else
            a.classList.remove("active");
    }
}
//endregion
//region CheckBoxes
document.addEventListener("DOMContentLoaded", function initCheckboxes() {
    for (var _i = 0, _a = document.querySelectorAll(".checkbox"); _i < _a.length; _i++) {
        var checkbox = _a[_i];
        setupCheckbox(checkbox);
    }
});
function setupCheckbox(checkbox, onToggle, asButton) {
    if (asButton === void 0) { asButton = false; }
    checkbox.tabIndex = 0;
    checkbox.addEventListener("click", function () {
        checkbox.focus();
        toggle();
    });
    checkbox.addEventListener("keypress", function (e) {
        if ([" ", "Space", "Spacebar"].indexOf(e.key) == -1)
            return;
        e.preventDefault();
        toggle();
    });
    var name = checkbox.getAttribute("name");
    if (name)
        register(name.split('.'), function (b) {
            checkbox.classList[b ? "add" : "remove"]("checked");
            onToggle && onToggle(b);
        });
    function toggle() {
        if (name) {
            var keys = name.split('.');
            keys.push(!checkbox.classList.contains("checked"));
            send(JSON.stringify(keys));
        }
        else if (asButton) {
            onToggle && onToggle(false);
        }
        else {
            var b = checkbox.classList.toggle("checked");
            onToggle && onToggle(b);
        }
    }
}
//endregion
//region KeyCombos
document.addEventListener("DOMContentLoaded", function initKeyCombos() {
    for (var _i = 0, _a = document.querySelectorAll(".keycombo"); _i < _a.length; _i++) {
        var element = _a[_i];
        var fragment = document.createDocumentFragment();
        var first = true;
        for (var _b = 0, _c = element.textContent.split(/ *\| */); _b < _c.length; _b++) {
            var combo = _c[_b];
            if (first)
                first = false;
            else
                fragment.appendChild(document.createTextNode(" | "));
            for (var _d = 0, _e = combo.split(/ *\+ */); _d < _e.length; _d++) {
                var key = _e[_d];
                var keyElement = document.createElement("span");
                keyElement.classList.add("key");
                keyElement.textContent = key;
                fragment.appendChild(keyElement);
            }
        }
        while (element.firstChild)
            element.removeChild(element.lastChild);
        element.appendChild(fragment);
    }
});
//endregion
//region RotateWASD
document.addEventListener("DOMContentLoaded", function () {
    var button = document.getElementById("rotateWasd");
    button.onclick = function () { return send(["Games", "Wasd", 0]); };
    var _loop_2 = function (checkbox) {
        setupCheckbox(checkbox, function () {
            send(["Games", "Wasd", +checkbox.getAttribute("value")]);
        }, true);
    };
    for (var _i = 0, _a = document.querySelectorAll(".rotateWasd .box"); _i < _a.length; _i++) {
        var checkbox = _a[_i];
        _loop_2(checkbox);
    }
    register(["Games", "Wasd"], function (i) {
        for (var _i = 0, _a = document.querySelectorAll(".rotateWasd .checked"); _i < _a.length; _i++) {
            var checked = _a[_i];
            checked.classList.remove("checked");
        }
        document.querySelector(".rotateWasd .box[value='" + i + "']").classList.add("checked");
        button.style.visibility = i == 0 ? "hidden" : "visible";
    });
});
//endregion
//region TextBoxes
document.addEventListener("DOMContentLoaded", function () {
    //Textarea autoheight
    for (var _i = 0, _a = document.querySelectorAll("textarea"); _i < _a.length; _i++) {
        var textarea = _a[_i];
        initTextArea(textarea);
    }
    var _loop_3 = function (input) {
        var systemValue = null; //If never received a value, then don't override on blur
        register(input.getAttribute("name").split('.'), function (v) {
            systemValue = input.classList.contains("color") ? v.toString(16).toUpperCase() : v.toString();
            if (document.activeElement == input)
                return; //Don't override while focused
            input.value = systemValue;
            // @ts-ignore
            if ("autoSize" in input)
                input.autoSize();
        });
        var sendValue = function (v) {
            var keys = input.getAttribute("name").split('.');
            keys.push(v);
            send(keys);
        };
        var inputFunction = function () {
            { //Restrict letters using regex
                var value = input.value;
                var start = input.selectionStart;
                var end = input.selectionEnd;
                var direction = input.selectionDirection;
                value = value.substring(0, start) + '\0' + value.substring(start, end) + '\0' + value.substring(end);
                if (input.classList.contains("color"))
                    value = value.toUpperCase().replace(/[^0-9A-F\x00]/, '');
                else if (input.classList.contains("int")) {
                    value = value.replace(/[^0-9\x00-]/, '');
                    var m = value.match(/^(\x00*-?)(.*)/);
                    value = m[1] + m[2].replace(/-/g, "");
                }
                start = value.indexOf('\0');
                end = value.lastIndexOf('\0') - 1;
                input.value = value.replace(/\x00/g, '');
                if (document.activeElement == input)
                    input.setSelectionRange(start, end, direction);
            }
            var error = false;
            if (input.classList.contains("color")) {
                if (/^[0-9A-Fa-f]{1,8}$/.test(input.value))
                    sendValue(parseInt(input.value, 16));
                else
                    error = true;
            }
            else if (input.classList.contains("int")) {
                if (/^-?[0-9]+$/.test(input.value)) {
                    var number = +input.value;
                    var min = +(input.getAttribute("min") || -0x7FFFFFFF);
                    var max = +(input.getAttribute("max") || 0x7FFFFFFF);
                    if (number >= min && number <= max)
                        sendValue(number);
                    else
                        error = true;
                }
                else
                    error = true;
            }
            else {
                sendValue(input.value);
            }
            input.classList[error ? "add" : "remove"]("error");
        };
        input.addEventListener("input", inputFunction);
        input.addEventListener("blur", function () {
            inputFunction();
            if (systemValue != null && input.value != systemValue)
                input.value = systemValue;
            input.classList.remove("error");
            // @ts-ignore
            if ("autoSize" in input)
                input.autoSize();
        });
    };
    for (var _b = 0, _c = document.querySelectorAll("input[name],textarea[name]"); _b < _c.length; _b++) {
        var input = _c[_b];
        _loop_3(input);
    }
});
function initTextArea(textarea) {
    if (textarea.parentElement.id == "test")
        return;
    function autoSize() {
        textarea.rows = 1;
        var parent = textarea.parentElement;
        var preStyle = parent.getAttribute("style");
        parent.style.height = parent.clientHeight + "px";
        textarea.style.height = "auto";
        textarea.style.height = textarea.scrollHeight + "px";
        if (preStyle)
            parent.setAttribute("style", preStyle);
        else
            parent.removeAttribute("style");
    }
    textarea.addEventListener("input", autoSize);
    // @ts-ignore
    textarea.autoSize = autoSize;
    autoSize();
}
//endregion
//region HotStrings
var map = [];
var blocked = [];
var root;
document.addEventListener("DOMContentLoaded", function () {
    root = new HotStringCategory(null, null);
    register(["HotStrings", "List"], function (j) {
        for (var i = root.childs.length - 1; i >= 0; i--)
            root.childs[i].destroy(true);
        for (var _i = 0, j_1 = j; _i < j_1.length; _i++) {
            var child = j_1[_i];
            root.addChild(getHotString(root, child));
        }
    });
    register(["Internal", "HotStrings", "Block"], function (j) {
        for (var _i = 0, blocked_1 = blocked; _i < blocked_1.length; _i++) {
            var hotString = blocked_1[_i];
            hotString.div.classList.remove("blocked");
        }
        blocked.length = 0;
        for (var _a = 0, j_2 = j; _a < j_2.length; _a++) {
            var i = j_2[_a];
            var hotString = getHotString(null, i);
            blocked.push(hotString);
            hotString.div.classList.add("blocked");
        }
    });
});
function getHotString(parent, child) {
    if (typeof child == "number")
        return map[child];
    if ("Category" in child)
        return new HotStringCategory(parent, child);
    if ("Emoji" in child)
        return new HotStringEmoji(parent, child);
    if ("Regex" in child)
        return new HotStringRegex(parent, child);
    return new HotStringReplace(parent, child);
}
function updateHotString(value) {
    var hotString = map[value.Id];
    if (!hotString)
        return getHotString(null, value);
    hotString.loadJson(value);
    return hotString;
}
function makeDragable(hs) {
    var element = hs.div;
    element.classList.add("drag");
    function reposition(e) {
        var posY = e.clientY;
        var nearest = Infinity;
        var inserter = null;
        root.forEach(hs, function (y, ins) {
            y = Math.abs(posY - y);
            if (y < nearest) {
                nearest = y;
                inserter = ins;
            }
        });
        //nearest element is itself. it's easier to check for null than to check inserter variable
        if (inserter == null)
            return;
        var prevParent = hs.parent;
        var prevChilds = prevParent.childs.slice();
        inserter(hs);
        if (hs.parent != prevParent) {
            hs.parent.send();
            return;
        }
        var afterChilds = hs.parent.childs;
        if (prevChilds.length != afterChilds.length) {
            hs.parent.send();
            return;
        }
        for (var i = 0; i < afterChilds.length; i++)
            if (prevChilds[i] != afterChilds[i]) {
                hs.parent.send();
                return;
            }
    }
    element.addEventListener("mousedown", function (e) {
        var _a, _b;
        if (e.target != element)
            return;
        e.preventDefault();
        e.stopPropagation();
        reposition(e);
        //element.focus();
        (_b = (_a = document.activeElement) === null || _a === void 0 ? void 0 : _a.blur) === null || _b === void 0 ? void 0 : _b.call(_a);
        element.classList.add("dragging");
        document.addEventListener("mousemove", reposition);
        function onMouseUp(e) {
            reposition(e);
            element.classList.remove("dragging");
            document.removeEventListener("mousemove", reposition);
            document.removeEventListener("mouseup", onMouseUp);
        }
        document.addEventListener("mouseup", onMouseUp);
    });
}
var HotString = /** @class */ (function () {
    function HotString(parent, data) {
        var _this = this;
        this.parent = null;
        this.options = [];
        if (data == null) {
            this.id = 0;
            this.div = document.querySelector("#hotstrings");
            return;
        }
        this.id = data.Id;
        map[this.id] = this;
        var isCategory = "Category" in data;
        this.div = document.createElement("div");
        this.div.classList.add(isCategory ? "category" : "hotstringContainer");
        makeDragable(this);
        { //Titlebar
            var title = document.createElement("div");
            this.div.appendChild(title).classList.add("title");
            var collapsed = document.createElement("div");
            collapsed.classList.add("collapsed");
            collapsed.classList.add("box");
            setupCheckbox(collapsed, function () {
                _this.div.classList.toggle("collapsed");
                _this.send();
            }, true);
            if (data.Collapsed != false)
                this.div.classList.add("collapsed");
            title.appendChild(collapsed);
            var enabled = document.createElement("div");
            enabled.classList.add("enabled");
            enabled.classList.add("box");
            setupCheckbox(enabled, function () {
                _this.div.classList.toggle("enabled");
                _this.send();
            }, true);
            if (data.Enabled != false)
                this.div.classList.add("enabled");
            title.appendChild(enabled);
            title.appendChild(this.errorBox = document.createElement("abbr")).textContent = "[ERROR]";
            this.titleText = document.createElement(isCategory ? "input" : "span");
            title.appendChild(this.titleText);
            if (!isCategory) {
                this.titleText.appendChild(document.createElement("span")); //Type
                this.titleText.appendChild(document.createTextNode(": "));
                var from = this.titleText.appendChild(document.createElement("span"));
                from.classList.add("hotstring");
                from.classList.add("from");
                this.titleText.appendChild(document.createTextNode(" ⇒ "));
                var to = this.titleText.appendChild(document.createElement("span"));
                to.classList.add("hotstring");
                to.classList.add("to");
            }
            var trash_1 = document.createElement("div");
            trash_1.classList.add("trash");
            trash_1.classList.add("box");
            setupCheckbox(trash_1, function (b) {
                if (b)
                    setTimeout(function () { return trash_1.classList.remove("checked"); }, 1000);
                else {
                    var parent_1 = _this.parent;
                    _this.destroy(true);
                    parent_1 && parent_1.send();
                    _this.send();
                }
            });
            trash_1.appendChild(document.createElement("div"));
            title.appendChild(trash_1);
        }
        if (parent)
            parent.addChild(this);
    }
    HotString.prototype.forEach = function (hs, func) {
        func(this.div.getBoundingClientRect().top, this == hs ? null : this.addBefore.bind(this));
    };
    HotString.prototype.addBefore = function (hs) {
        hs.destroy(false);
        if (!this.parent)
            return;
        var i = this.parent.childs.indexOf(this);
        if (i != -1)
            this.parent.childs.splice(i, 0, hs);
        hs.parent = this.parent;
        this.parent.div.insertBefore(hs.div, this.div);
    };
    HotString.prototype.destroy = function (removeHtml) {
        var _a;
        if (this.parent == null)
            return;
        var i = this.parent.childs.indexOf(this);
        if (i != -1)
            this.parent.childs.splice(i, 1);
        if (removeHtml)
            (_a = this.div.parentElement) === null || _a === void 0 ? void 0 : _a.removeChild(this.div);
        this.parent = null;
    };
    HotString.prototype.send = function () {
        this.updateControls();
        if (this.parent == null) {
            var id = this.id;
            delete map[id];
            send(id);
        }
        else {
            var json = this.toJson();
            json.Enabled = this.div.classList.contains("enabled") && undefined;
            json.Collapsed = this.div.classList.contains("collapsed") && undefined;
            json.Id = this.id;
            send(json);
        }
    };
    HotString.prototype.loadJson = function (json) {
        for (var _i = 0, _a = this.options; _i < _a.length; _i++) {
            var _b = _a[_i], input = _b[0], func = _b[1];
            var value = func(json);
            if (typeof value == "boolean")
                input.classList[value ? "add" : "remove"]("checked");
            else
                input.value = value || "";
        }
        this.div.classList[json.Enabled != false ? "add" : "remove"]("enabled");
        this.div.classList[json.Collapsed != false ? "add" : "remove"]("collapsed");
        if (this.errorBox) {
            if (json.Error)
                this.errorBox.title = json.Error;
            else
                this.errorBox.removeAttribute("title");
        }
        this.updateControls();
    };
    HotString.prototype.updateControls = function () {
        var arr = this.getFromTo();
        this.titleText.children[0].textContent = arr[0];
        this.titleText.querySelector(".from").textContent = arr[1];
        this.titleText.querySelector(".to").textContent = arr[2];
    };
    HotString.prototype.addOption = function (func, name, sub) {
        var _this = this;
        var row = this.div.appendChild(document.createElement("tr"));
        var nameEleemnt = row.appendChild(document.createElement("td"));
        nameEleemnt.textContent = name;
        if (sub)
            nameEleemnt.appendChild(document.createElement("sub")).textContent = sub;
        var input;
        if (typeof (func({})) == "boolean") {
            input = document.createElement("div");
            input.classList.add("checkbox");
            setupCheckbox(input, function () {
                _this.updateControls();
                _this.send();
            });
        }
        else {
            input = document.createElement("input");
            input.addEventListener("blur", function () { return send(["Internal", "HotStrings", "Block", 0]); });
            input.addEventListener("focus", function () { return send(["Internal", "HotStrings", "Block", _this.id]); });
            input.addEventListener("input", function () {
                _this.updateControls();
                _this.send();
            });
        }
        row.appendChild(document.createElement("td")).appendChild(input);
        this.options.push([input, func]);
        return input;
    };
    return HotString;
}());
var HotStringCategory = /** @class */ (function (_super) {
    __extends(HotStringCategory, _super);
    function HotStringCategory(parent, data) {
        var _this = _super.call(this, parent, data) || this;
        _this.childs = [];
        if (data == null) {
            _this.addBefore = function () {
            };
        }
        else {
            _this.titleText.value = data.Category;
            _this.titleText.addEventListener("input", function () { return _this.send(); }); //TODO maybe fix: will be overridden even when focused
        }
        for (var _i = 0, _a = ((data && data.Children) || []); _i < _a.length; _i++) {
            var child = _a[_i];
            _this.addChild(getHotString(_this, child));
        }
        _this.div.appendChild(_this.createNew = document.createElement("div")).classList.add("createNew");
        var addOption = function (name, create) {
            var div = _this.createNew.appendChild(document.createElement("div"));
            div.textContent = name;
            div.addEventListener("click", function () {
                create().send();
                _this.send();
            });
        };
        addOption("Category", function () { return new HotStringCategory(_this, {
            Id: Date.now(),
            Category: "Category",
            Collapsed: false,
        }); });
        addOption("Emoji", function () { return new HotStringEmoji(_this, {
            Id: Date.now(),
            From: "xdd",
            Emoji: "😂",
            Collapsed: false,
        }); });
        addOption("Regex", function () { return new HotStringRegex(_this, {
            Id: Date.now(),
            Regex: "(?<=^| )itn$",
            Replacement: "int",
            IgnoreCase: false,
            Collapsed: false,
        }); });
        addOption("Replace", function () { return new HotStringReplace(_this, {
            Id: Date.now(),
            From: "cosnt",
            To: "const",
            IgnoreCase: false,
            Collapsed: false,
        }); });
        return _this;
    }
    HotStringCategory.prototype.forEach = function (hs, func) {
        if (this == hs) {
            if (this.parent != null)
                func(this.div.getBoundingClientRect().top, null);
            return;
        }
        if (this.parent)
            func(this.div.getBoundingClientRect().top, this.addBefore.bind(this));
        if (this.div.classList.contains("collapsed"))
            return;
        for (var _i = 0, _a = this.childs; _i < _a.length; _i++) {
            var child = _a[_i];
            child.forEach(hs, func);
        }
        func(this.createNew.getBoundingClientRect().top, this.addChild.bind(this));
    };
    HotStringCategory.prototype.addChild = function (hs) {
        hs.destroy(false);
        this.childs.push(hs);
        hs.parent = this;
        this.div.insertBefore(hs.div, this.createNew);
    };
    HotStringCategory.prototype.send = function () {
        if (this.id == 0)
            return send(["HotStrings", "List", this.childs.map(function (c) { return c.id; })]);
        if (this.parent == null) {
            //freeing children
            for (var i = this.childs.length - 1; i >= 0; i--) {
                var child = this.childs[i];
                child.destroy(true);
                child.send();
            }
        }
        _super.prototype.send.call(this);
    };
    HotStringCategory.prototype.loadJson = function (json) {
        _super.prototype.loadJson.call(this, json);
        this.titleText.value = json.Category;
        var arr = json.Children || [];
        for (var i = this.childs.length - 1; i >= 0; i--)
            this.childs[i].destroy(arr.indexOf(this.childs[i].id) == -1); //only remove html if needed
        for (var _i = 0, arr_1 = arr; _i < arr_1.length; _i++) {
            var child = arr_1[_i];
            this.addChild(getHotString(null, child));
        }
    };
    HotStringCategory.prototype.toJson = function () {
        return {
            Category: this.titleText.value,
            Children: this.childs.map(function (c) { return c.id; }),
        };
    };
    HotStringCategory.prototype.getFromTo = function () {
        return ["", "", ""];
    };
    HotStringCategory.prototype.updateControls = function () {
    };
    return HotStringCategory;
}(HotString));
var HotStringEmoji = /** @class */ (function (_super) {
    __extends(HotStringEmoji, _super);
    function HotStringEmoji(parent, data) {
        var _this = _super.call(this, parent, data) || this;
        _this._from = _this.addOption(function (j) { return j.From; }, "From");
        _this._regex = _this.addOption(function (j) { return j.Regex; }, "Regex", "(optional)");
        _this._ignoreCase = _this.addOption(function (j) { return j.IgnoreCase == true; }, "IgnoreCase");
        _this._emoji = _this.addOption(function (j) { return j.Emoji; }, "Emoji");
        _this._emoji.classList.add("emoji");
        _this.loadJson(data);
        return _this;
    }
    HotStringEmoji.prototype.toJson = function () {
        return {
            From: this._from.value,
            Emoji: this._emoji.value,
            Regex: this._regex.value || undefined,
            IgnoreCase: this._ignoreCase.classList.contains("checked") || undefined
        };
    };
    HotStringEmoji.prototype.getFromTo = function () {
        return ["Emoji", this._from.value, this._emoji.value];
    };
    return HotStringEmoji;
}(HotString));
var HotStringRegex = /** @class */ (function (_super) {
    __extends(HotStringRegex, _super);
    function HotStringRegex(parent, data) {
        var _this = _super.call(this, parent, data) || this;
        _this._regex = _this.addOption(function (j) { return j.Regex; }, "Regex");
        _this._ignoreCase = _this.addOption(function (j) { return j.IgnoreCase == true; }, "IgnoreCase");
        _this._replacement = _this.addOption(function (j) { return j.Replacement; }, "Replacement");
        _this.loadJson(data);
        return _this;
    }
    HotStringRegex.prototype.toJson = function () {
        return {
            Regex: this._regex.value,
            IgnoreCase: this._ignoreCase.classList.contains("checked"),
            Replacement: this._replacement.value
        };
    };
    HotStringRegex.prototype.getFromTo = function () {
        return ["Regex", this._regex.value, this._replacement.value];
    };
    return HotStringRegex;
}(HotString));
var HotStringReplace = /** @class */ (function (_super) {
    __extends(HotStringReplace, _super);
    function HotStringReplace(parent, data) {
        var _this = _super.call(this, parent, data) || this;
        _this._from = _this.addOption(function (j) { return j.From; }, "From");
        _this._ignoreCase = _this.addOption(function (j) { return j.IgnoreCase == true; }, "IgnoreCase");
        _this._to = _this.addOption(function (j) { return j.To; }, "To");
        _this._keepCase = _this.addOption(function (j) { return j.KeepCase != false; }, "KeepCase");
        _this.loadJson(data);
        return _this;
    }
    HotStringReplace.prototype.updateControls = function () {
        _super.prototype.updateControls.call(this);
        if (this._from.value.length == this._to.value.length) {
            this._keepCase.removeAttribute("disabled");
        }
        else {
            this._keepCase.setAttribute("disabled", "true");
            this._keepCase.classList.remove("checked");
        }
        if (this._keepCase.classList.contains("checked")) {
            this._ignoreCase.setAttribute("disabled", "true");
            this._keepCase.classList.add("checked");
        }
        else
            this._ignoreCase.removeAttribute("disabled");
    };
    HotStringReplace.prototype.toJson = function () {
        return {
            From: this._from.value,
            IgnoreCase: this._ignoreCase.classList.contains("checked") || undefined,
            To: this._to.value,
            KeepCase: this._from.value.length == this._to.value.length && !this._keepCase.classList.contains("checked") ? false : undefined
        };
    };
    HotStringReplace.prototype.getFromTo = function () {
        return ["Replace", this._from.value, this._to.value];
    };
    return HotStringReplace;
}(HotString));
//endregion
//region Test Area
document.addEventListener("DOMContentLoaded", function () {
    var textarea = document.querySelector("#test>textarea");
    var div = document.querySelector("#test>div");
    var input = function () {
        var content = textarea.value;
        //replace surrogate pairs with single char
        var contentLength = content.replace(/[\uD800-\uDBFF][\uDC00-\uDFFF]/g, '_').length;
        div.textContent =
            "Length: ".concat(contentLength, "\n") +
                "UTF-16: ".concat(content.length, "\n") +
                "Lines: ".concat(content.split(/\r\n|\r|\n/).length);
    };
    textarea.addEventListener("input", input);
    input();
});
//endregion
//# sourceMappingURL=script.js.map