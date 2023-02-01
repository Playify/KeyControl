var __extends=(this&&this.__extends)||(function(){
	var extendStatics=function(d,b){
		extendStatics=Object.setPrototypeOf||
			({__proto__:[]} instanceof Array&&function(d,b){
				d.__proto__=b;
			})||
			function(d,b){
				for(var p in b) if(Object.prototype.hasOwnProperty.call(b,p)) d[p]=b[p];
			};
		return extendStatics(d,b);
	};
	return function(d,b){
		if(typeof b!=="function"&&b!==null)
			throw new TypeError("Class extends value "+String(b)+" is not a constructor or null");
		extendStatics(d,b);

		function __(){
			this.constructor=d;
		}

		d.prototype=b===null?Object.create(b):(__.prototype=b.prototype, new __());
	};
})();
//region Dark Mode
// noinspection JSUnusedGlobalSymbols
function setDarkMode(b){
	localStorage===null||localStorage=== void 0?void 0:localStorage.setItem("dark",""+b);
	if(b)
		document.documentElement.classList.add("dark");
	else
		document.documentElement.classList.remove("dark");
	return true;
}

{
	var wantsDark=void 0;
	var storageDark=localStorage===null||localStorage=== void 0?void 0:localStorage.getItem("dark");
	if(storageDark=="true"||storageDark=="false")
		wantsDark=storageDark=="true";
	else
		wantsDark=window.matchMedia('(prefers-color-scheme: dark)').matches;
	//don't add event listener, because C# Form also doesn't respond to changes
	if(wantsDark)
		document.documentElement.classList.add("dark");
	else
		document.documentElement.classList.remove("dark");
}
// noinspection JSDeprecatedSymbols
var ownExternal=(external===null||external=== void 0?void 0:external.Available)?external:null;
//endregion
//region Hash
function initNavigation(){
	if(!ownExternal)
		return;
	var as=document.querySelectorAll("nav>a");
	var _loop_1=function(i){
		var a=as[i];
		//remove href to avoid creation of history
		var href=a.getAttribute("href"); /*
        a.removeAttribute("href");
        a.tabIndex=0;*/
		a.onclick=function(e){
			ownExternal.Hash=href;
			e.preventDefault();
			onHashChange();
		};
	};
	for(var i=0; i<as.length; i++){
		_loop_1(i);
	}
}

function onHashChange(){
	var _a;
	var defaultHash="#hotkeys";
	var hash=(_a=ownExternal===null||ownExternal=== void 0?void 0:ownExternal.Hash)!==null&&_a!== void 0?_a:document.location.hash;
	var sections=document.querySelectorAll("main>*");
	var found=hash==defaultHash;
	for(var i=0; i<sections.length; i++){
		var section=sections[i];
		var b=("#"+section.id)==hash;
		if(b)
			section.classList.add("active");
		else
			section.classList.remove("active");
		if(b)
			found=true;
	}
	if(!found){
		if(ownExternal)
			ownExternal.Hash=defaultHash;
		else
			document.location.hash=defaultHash;
		onHashChange();
		return;
	}
	var as=document.querySelectorAll("nav>a");
	for(var i=0; i<as.length; i++){
		var a=as.item(i);
		if(a.getAttribute("href")==hash)
			a.classList.add("active");
		else
			a.classList.remove("active");
	}
}

window.onhashchange=onHashChange;

//endregion
function testInfo(){
	var testA=document.querySelector("nav>a[href=\"#test\"] sub");
	var test=document.getElementById("test");
	test.focus();
	var content=test.value;
	//replace surrogate pairs with single char
	var contentLength=content.replace(/[\uD800-\uDBFF][\uDC00-\uDFFF]/g,'_').length;
	var text="Length: ".concat(contentLength," (UTF-16: ").concat(content.length,")\n")+
		"Lines: ".concat(content.split(/\r\n|\r|\n/).length);
	testA.setAttribute("title",text);
}

//region WebSocket
var loaded=false;
var pending=[];
var ws=null;
var _send;
if(ownExternal)
	_send=ownExternal.Init(receive);
else
	send(null);

function send(s){
	if(s!=null&&_send!=null){
		_send.Send(s);
		return;
	}
	if(s!=null)
		pending.push(s);
	if(ws&&ws.readyState==WebSocket.OPEN){
		if(!pending.length)
			return;
		s=pending.shift();
		ws.send(s);
		setTimeout(send,0);
	}else if(!ws||ws.readyState==WebSocket.CLOSING||ws.readyState==WebSocket.CLOSED){
		/*let url=document.URL;
		url="ws"+url.substring(4);
		const i=url.indexOf('#');
		if(i!= -1) url=url.substring(0,i);
		const i2=url.indexOf('?');
		if(i2!= -1) url=url.substring(0,i2);
		ws=new WebSocket(""+url);//*/
		var url_1="ws"+new URL(document.URL).origin.substring(4);
		ws=new WebSocket(url_1);
		ws.onclose=function(){
			ws.close();
			setTimeout(send,100);
		};
		ws.onerror=function(){
			ownExternal===null||ownExternal=== void 0?void 0:ownExternal.Log("Error connecting to Websocket: "+url_1);
			ws.close();
			setTimeout(send,100);
		};
		ws.onmessage=receive;
		ws.onopen=function(){
			setTimeout(send,0);
		};
	}
}

function receive(e){
	var s=typeof e=="string"?e:e.data;
	if(typeof s!=="string"){
		console.error("Error reading WebSocket Data: ",s);
		return;
	}
	console.log("RECV:"+s);
	var indexOf=s.indexOf('=');
	var key=s.substring(0,indexOf);
	var valueStr=s.substring(indexOf+1);
	var value=JSON.parse(valueStr);
	if(key=="Random"){
		randomID=value;
	}else if(key=="hotString"){
		HotString.update(value);
	}else if(key=="hotStringBlock"){
		HotString.updateBlocked(value);
	}else if(key.indexOf("err:")==0){
		var elements=document.getElementsByName(key.substring(4));
		for(var i=0; i<elements.length; i++){
			var element=elements[i];
			element.classList.add("error");
			element.title=value;
		}
	}else{
		var elements=document.getElementsByName(key);
		var startsWith=document.querySelectorAll("[name^=\""+key+"=\"]");
		if(elements.length==0&&startsWith.length==0){
			console.error("Unknown Key: "+key);
			return;
		}
		for(var i=0; i<startsWith.length; i++){
			var element=startsWith[i];
			var elementValue=element.getAttribute("name").substring(key.length+1);
			if(element.type=="checkbox")
				element.checked=elementValue==valueStr;
			else
				element.value=elementValue==valueStr?value:"";
			updateElement(element);
		}
		for(var i=0; i<elements.length; i++){
			var element=elements[i];
			if(element.type=="checkbox")
				element.checked=value;
			else
				element.value=value;
			updateElement(element);
		}
	}
}

//endregion
document.addEventListener("DOMContentLoaded",function(){
	initNavigation();
	document.getElementById("test").oninput=testInfo;
	testInfo();
	loaded=true;
	var checkboxes=document.querySelectorAll(".check.box,.box[name*=\"=\"]");
	for(var i=0; i<checkboxes.length; i++)
		setupCheckbox(checkboxes[i]);
	onHashChange();
	initKeyCombos();
	initChangeListener();
	Category.master; //enforce loading of Master
});
//region Rainbow
{
	var curr_1="";
	var konami_1="uuddlrlrba";
	document.addEventListener("keydown",function(e){
		var k;
		switch(e.key){
			case "Escape":
			case "Esc":
				ownExternal===null||ownExternal=== void 0?void 0:ownExternal.Close();
				return;
			case "Up":
				k="u";
				break;
			case "Down":
				k="d";
				break;
			case "Left":
				k="l";
				break;
			case "Right":
				k="r";
				break;
			case "ArrowUp":
				k="u";
				break;
			case "ArrowDown":
				k="d";
				break;
			case "ArrowLeft":
				k="l";
				break;
			case "ArrowRight":
				k="r";
				break;
			case "a":
			case "A":
				k="a";
				break;
			case "b":
			case "B":
				k="b";
				break; //handled by other
			default:
				k="";
				break;
		}
		if(!k)
			curr_1="";
		curr_1+=k;
		if(curr_1.length>konami_1.length)
			curr_1=curr_1.substring(curr_1.length-konami_1.length);
		if(curr_1==konami_1)
			document.documentElement.classList.toggle("rainbow");
	});
}
//endregion
//region HotStrings
var randomID=Math.floor(Math.random()*(-1>>>0)); //Number.MAX_SAFE_INTEGER
function getRandomId(){
	while(HotString.IdMap[++randomID]){
	}
	return randomID;
}

function makeDraggable(hs){
	var element=hs.div;
	element.classList.add("drag");

	function reposition(e){
		var posY=e.clientY;
		var nearest=Infinity;
		var inserter=null;
		Category.master.forEach(hs,function(y,func /*,currHs,el,pos*/){
			y=Math.abs(posY-y);
			if(y<nearest){
				nearest=y;
				inserter=func;
			}
		});
		//nearest element is itself. its easier to check for null than to check inserter variable
		if(inserter==null)
			return;
		var prevParent=hs.parent;
		var prevChilds=prevParent.childs.slice();
		inserter(hs);
		if(hs.parent!=prevParent){
			hs.parent.send();
			prevParent.send();
			return;
		}
		var afterChilds=hs.parent.childs;
		if(prevChilds.length!=afterChilds.length){
			hs.parent.send();
			return;
		}
		for(var i=0; i<afterChilds.length; i++)
			if(prevChilds[i]!=afterChilds[i]){
				hs.parent.send();
				return;
			}
	}

	element.addEventListener("mousedown",function(e){
		var _a,_b;
		if(e.target!=element)
			return;
		e.preventDefault();
		e.stopPropagation();
		reposition(e);
		//element.focus();
		(_b=(_a=document.activeElement)===null||_a=== void 0?void 0:_a.blur)===null||_b=== void 0?void 0:_b.call(_a);
		element.classList.add("dragging");

		function onMouseMove(e){
			reposition(e);
		}

		document.addEventListener("mousemove",onMouseMove);

		function onMouseUp(e){
			reposition(e);
			element.classList.remove("dragging");
			document.removeEventListener("mousemove",onMouseMove);
			document.removeEventListener("mouseup",onMouseUp);
		}

		document.addEventListener("mouseup",onMouseUp);
	});
}

var HotString= /** @class */ (function(){
	function HotString(parent,data){
		var _this=this;
		this.parent=null;
		this.options=[];
		if(data==null){
			this.id=0;
			this.div=document.querySelector("#hotstrings");
			return;
		}
		this.id=data.Id;
		HotString.IdMap[this.id]=this;
		this.div=document.createElement("div");
		var isCategory="Category" in data;
		this.div.classList.add(isCategory?"category":"hotstringContainer");
		makeDraggable(this);
		{
			var title=document.createElement("div");
			this.div.appendChild(title);
			title.classList.add("title");
			this._collapsed=document.createElement("div");
			title.appendChild(this._collapsed);
			this._collapsed.classList.add("collapsed");
			setupCheckbox(this._collapsed,this.div,"collapsed");
			this._collapsed.addEventListener("input",function(){
				//(<HTMLElement>document.activeElement)?.blur?.();
				_this.send();
			});
			this._collapsed.checked=data.Collapsed;
			this._enabled=document.createElement("div");
			title.appendChild(this._enabled);
			this._enabled.classList.add("enabled");
			setupCheckbox(this._enabled,this.div,"enabled");
			this._enabled.addEventListener("input",function(){
				_this.send();
			});
			this._enabled.checked=data.Enabled!=false;
			//if(data.Enabled==false) this.div.classList.add("enabled");
			this.errorBox=document.createElement("abbr");
			title.appendChild(this.errorBox);
			this.errorBox.textContent="[ERROR]";
			this.titleText=document.createElement(isCategory?"input":"span");
			title.appendChild(this.titleText);
			if(!isCategory){
				var type=document.createElement("span");
				this.titleText.appendChild(type);
				type.classList.add("type");
				var doubleDot=document.createTextNode(": ");
				this.titleText.appendChild(doubleDot);
				var hotstring=document.createElement("span");
				this.titleText.appendChild(hotstring);
				hotstring.classList.add("hotstring");
				var arrow=document.createTextNode(" ⇒ ");
				this.titleText.appendChild(arrow);
				var replacement=document.createElement("span");
				this.titleText.appendChild(replacement);
				replacement.classList.add("replacement");
			}
			var destroyer_1=document.createElement("div");
			title.appendChild(destroyer_1);
			destroyer_1.classList.add("delete");
			destroyer_1.classList.add("box");
			destroyer_1.addEventListener("click",function(){
				if(!destroyer_1.classList.contains("r_u_sure")){
					destroyer_1.classList.add("r_u_sure");
					setTimeout(function(){
						return destroyer_1.classList.remove("r_u_sure");
					},1000);
					return;
				}
				var p=_this.parent;
				_this.destroy();
				p.send();
				_this.send();
			});
			var trash=document.createElement("div");
			trash.classList.add("trash");
			destroyer_1.appendChild(trash);
		}
		parent===null||parent=== void 0?void 0:parent.addChild(this);
		this.addBefore=function(hs){
			hs.destroy();
			var i=_this.parent.childs.indexOf(_this);
			if(i== -1)
				console.error("HotString is not child of parent");
			else
				_this.parent.childs.splice(i,0,hs);
			hs.parent=_this.parent;
			_this.parent.div.insertBefore(hs.div,_this.div);
		};
	}

	HotString.update=function(value){
		var _a;
		if(typeof value=="number"){
			(_a=this.IdMap[value])===null||_a=== void 0?void 0:_a.destroy();
			delete this.IdMap[value];
			return;
		}
		if(Array.isArray(value)){
			Category.master.loadJson(value);
			return;
		}
		var id=value.Id;
		var old=this.IdMap[id];
		if(old)
			old.loadJson(value);
		else
			this.get(null,value);
	};
	HotString.updateBlocked=function(value){
		for(var _i=0,_a=this.Blocked; _i<_a.length; _i++){
			var hotString=_a[_i];
			hotString.div.classList.remove("blocked");
		}
		this.Blocked=[];
		for(var _b=0,value_1=value; _b<value_1.length; _b++){
			var number=value_1[_b];
			var hotString=this.get(null,number);
			hotString.div.classList.add("blocked");
			this.Blocked.push(hotString);
		}
	};
	HotString.get=function(parent,child){
		if(typeof child=="number")
			return this.IdMap[child];
		if("Category" in child)
			return new Category(parent,child);
		if("Emoji" in child)
			return new HotStringEmoji(parent,child);
		if("Regex" in child)
			return new HotStringRegex(parent,child);
		/*const from=<string>child.From;
		const to=<string>child.To;
		if(typeof from!=="string") throw new Error("From is null");
		if(typeof to!=="string") throw new Error("To is null");
		if(from.length!=to.length) return new HotStringReplace(parent,child);
		if(child.keepCase!=false) return new HotStringKeepCase(parent,child);*/
		return new HotStringReplace(parent,child);
	};
	HotString.prototype.addOption=function(func,name,sub){
		var _this=this;
		var row=document.createElement("tr");
		this.div.appendChild(row);
		var nameElement=document.createElement("td");
		row.appendChild(nameElement);
		nameElement.textContent=name;
		if(sub){
			var subElement=document.createElement("sub");
			subElement.textContent=sub;
			nameElement.appendChild(subElement);
		}
		var inputContainer=document.createElement("td");
		row.appendChild(inputContainer);
		var input;
		//checks type with empty object. If this control should be a checkbox a boolean is returned otherwise any gibberish means text
		if(typeof (func({}))==="boolean"){
			input=document.createElement("div");
			setupCheckbox(input);
		}else{
			input=document.createElement("input");
			input.addEventListener("blur",function(){
				send("hotStringBlock="+0);
			});
			input.addEventListener("focus",function(){
				send("hotStringBlock="+_this.id);
			});
		}
		input.addEventListener("input",function(){
			_this.updateControls();
			_this.send();
		});
		inputContainer.appendChild(input);
		this.options.push([input,func]);
		return input;
	};
	HotString.prototype.forEach=function(hs,func){
		if(this==hs)
			func(this.div.getBoundingClientRect().top,null,this,this.div,"self hs");
		else
			func(this.div.getBoundingClientRect().top,this.addBefore,this,this.div,"before");
	};
	HotString.prototype.destroy=function(removeHtml){
		if(removeHtml=== void 0){
			removeHtml=true;
		}
		if(this.parent==null)
			return;
		var i=this.parent.childs.indexOf(this);
		if(i== -1)
			console.error("HotString is not child of parent");
		else
			this.parent.childs.splice(i,1);
		if(removeHtml)
			this.div.parentElement.removeChild(this.div);
		this.parent=null;
	};
	HotString.prototype.toFullJson=function(){
		var o=this.toJson();
		o.Enabled=(this.div.classList.contains("enabled"))&&undefined;
		o.Collapsed=this.div.classList.contains("collapsed")||undefined;
		o.Error=this.errorBox.title||undefined;
		o.Id=this.id;
		return o;
	};
	HotString.prototype.loadJson=function(json){
		for(var _i=0,_a=this.options; _i<_a.length; _i++){
			var _b=_a[_i],input=_b[0],func=_b[1];
			var value=func(json);
			if(typeof value=="boolean")
				input.checked=value;
			else
				input.value=value||"";
		}
		if(this.errorBox){
			if(json.Error)
				this.errorBox.title=json.Error;
			else
				this.errorBox.removeAttribute("title");
			this._enabled.checked=json.Enabled!=false;
			//else this.div.classList.remove("disabled");
			if(json.Collapsed)
				this.div.classList.add("collapsed");
			else
				this.div.classList.remove("collapsed");
		}
		this.updateControls();
	};
	HotString.prototype.send=function(){
		this.updateControls();
		var value;
		if(this.parent==null){
			value=this.id;
			delete HotString.IdMap[this.id];
		}else
			value=this.toFullJson();
		send("hotString="+JSON.stringify(value));
	};
	HotString.prototype.updateControls=function(){
		var _a=this.getFromTo(),type=_a[0],from=_a[1],to=_a[2];
		this.titleText.querySelector(".type").textContent=type;
		this.titleText.querySelector(".hotstring").textContent=from;
		this.titleText.querySelector(".replacement").textContent=to;
	};
	HotString.IdMap=[];
	HotString.Blocked=[];
	return HotString;
}());
var Category= /** @class */ (function(_super){
	__extends(Category,_super);

	function Category(parent,data){
		var _this=_super.call(this,parent,data)||this;
		_this.childs=[];
		if(data==null){
			_this.addBefore=function(hs){
				console.log("can't add HotString before Master: ",hs);
			};
			var thiz_1=_this;
			Object.defineProperty(_this.div,"value",{
				get:function(){
					thiz_1.toJsonArray();
				},
				set:function(v){
					thiz_1.loadJson(v);
				}
			});
		}else{
			_this.titleText.value=data.Category;
			_this.titleText.addEventListener("input",function(){
				_this.send();
			});
			_this.addBefore=function(hs){
				hs.destroy(false);
				var i=_this.parent.childs.indexOf(_this);
				if(i== -1)
					console.error("Category is not child of parent");
				else
					_this.parent.childs.splice(i,0,hs);
				hs.parent=_this.parent;
				_this.parent.div.insertBefore(hs.div,_this.div);
			};
		}
		_this.createNew=document.createElement("div");
		_this.div.appendChild(_this.createNew);
		_this.createNew.classList.add("addNew");
		var addOption=function(name,create){
			var div=document.createElement("div");
			_this.createNew.appendChild(div);
			div.textContent=name;
			div.addEventListener("click",function(){
				create().send();
				_this.send();
			});
		};
		addOption("Category",function(){
			return new Category(_this,{
				Id:getRandomId(),
				Category:"Category"
			});
		});
		addOption("Emoji",function(){
			return new HotStringEmoji(_this,{
				Id:getRandomId(),
				From:"xdd",
				Emoji:"😂"
			});
		});
		addOption("Regex",function(){
			return new HotStringRegex(_this,{
				Id:getRandomId(),
				Regex:"(?<=^| )itn$",
				Replacement:"int",
				IgnoreCase:false
			});
		});
		addOption("Replace",function(){
			return new HotStringReplace(_this,{
				Id:getRandomId(),
				From:"cosnt",
				To:"const",
				IgnoreCase:false
			});
		});
		_this.addChild=function(hs){
			hs.destroy(false);
			_this.childs.push(hs);
			hs.parent=_this;
			_this.div.insertBefore(hs.div,_this.createNew);
		};
		var childs=data===null||data=== void 0?void 0:data.Children;
		if(childs!=null)
			for(var i=0; i<childs.length; i++)
				_this.addChild(HotString.get(_this,childs[i]));
		return _this;
	}

	Object.defineProperty(Category,"master",{
		get:function(){
			if(this._master==null)
				this._master=new Category(null,null);
			return this._master;
		},
		enumerable:false,
		configurable:true
	});
	Category.prototype.destroy=function(){
		if(this.parent==null)
			return;
		var i=this.parent.childs.indexOf(this);
		if(i== -1)
			console.error("Category is not child of parent");
		else
			this.parent.childs.splice(i,1);
		//this.div.remove();
		this.div.parentElement.removeChild(this.div);
		this.parent=null;
	};
	Category.prototype.forEach=function(hs,func){
		if(this==hs){
			if(this.parent!=null)
				func(this.div.getBoundingClientRect().top,null,this,this.div,"self category");
			return;
		}
		if(this.parent!=null)
			func(this.div.getBoundingClientRect().top,this.addBefore,this,this.div,"before");
		if(this.div.classList.contains("collapsed"))
			return;
		for(var _i=0,_a=this.childs; _i<_a.length; _i++){
			var child=_a[_i];
			child.forEach(hs,func);
		}
		func(this.createNew.getBoundingClientRect().top,this.addChild,this,this.div,"child");
	};
	Category.prototype.toJson=function(){
		return this==Category.master?this.toJsonArray():{
			Category:this.titleText.value,
			Children:this.childs.length?this.toJsonArray():undefined
		};
	};
	Category.prototype.toJsonArray=function(){
		var arr=[];
		for(var _i=0,_a=this.childs; _i<_a.length; _i++){
			var child=_a[_i];
			arr.push(child.id);
		}
		return arr;
	};
	Category.prototype.loadJson=function(json){
		_super.prototype.loadJson.call(this,json);
		if(!Array.isArray(json)){
			this.titleText.value=json.Category;
			json=json.Children;
		}
		var arr=json?json:[];
		for(var i=this.childs.length-1; i>=0; i--)
			this.childs[i].destroy(arr.indexOf(this.childs[i].id)== -1); //only remove html if needed
		for(var _i=0,arr_1=arr; _i<arr_1.length; _i++){
			var child=arr_1[_i];
			this.addChild(HotString.get(this,child));
		}
	};
	Category.prototype.send=function(){
		var value;
		if(this==Category.master)
			value=this.toJsonArray();
		else if(this.parent==null){
			delete HotString.IdMap[this.id];
			value=this.id;
			//freeing children
			for(var i=this.childs.length-1; i>=0; i--){
				var child=this.childs[i];
				child.destroy();
				child.send();
			}
		}else
			value=this.toFullJson();
		send("hotString="+JSON.stringify(value));
	};
	Category.prototype.getFromTo=function(){
		return [null,null,null];
	};
	Category.prototype.updateControls=function(){
	};
	return Category;
}(HotString));
var HotStringEmoji= /** @class */ (function(_super){
	__extends(HotStringEmoji,_super);

	function HotStringEmoji(parent,data){
		var _this=_super.call(this,parent,data)||this;
		_this._from=_this.addOption(function(j){
			return j.From;
		},"From");
		_this._regex=_this.addOption(function(j){
			return j.Regex;
		},"Regex","(optional)");
		_this._ignoreCase=_this.addOption(function(j){
			return j.IgnoreCase==true;
		},"IgnoreCase");
		_this._emoji=_this.addOption(function(j){
			return j.Emoji;
		},"Emoji");
		_this._emoji.classList.add("emoji");
		_this.loadJson(data);
		return _this;
	}

	HotStringEmoji.prototype.toJson=function(){
		return {
			From:this._from.value,
			Emoji:this._emoji.value,
			Regex:this._regex.value||undefined,
			IgnoreCase:this._ignoreCase.checked||undefined
		};
	};
	HotStringEmoji.prototype.getFromTo=function(){
		return ["Emoji",this._from.value,this._emoji.value];
	};
	return HotStringEmoji;
}(HotString));
var HotStringRegex= /** @class */ (function(_super){
	__extends(HotStringRegex,_super);

	function HotStringRegex(parent,data){
		var _this=_super.call(this,parent,data)||this;
		_this._regex=_this.addOption(function(j){
			return j.Regex;
		},"Regex");
		_this._ignoreCase=_this.addOption(function(j){
			return j.IgnoreCase==true;
		},"IgnoreCase");
		_this._replacement=_this.addOption(function(j){
			return j.Replacement;
		},"Replacement");
		_this.loadJson(data);
		return _this;
	}

	HotStringRegex.prototype.toJson=function(){
		return {
			Regex:this._regex.value,
			IgnoreCase:this._ignoreCase.checked,
			Replacement:this._replacement.value
		};
	};
	HotStringRegex.prototype.getFromTo=function(){
		return ["Regex",this._regex.value,this._replacement.value];
	};
	return HotStringRegex;
}(HotString));
var HotStringReplace= /** @class */ (function(_super){
	__extends(HotStringReplace,_super);

	function HotStringReplace(parent,data){
		var _this=_super.call(this,parent,data)||this;
		_this._from=_this.addOption(function(j){
			return j.From;
		},"From");
		_this._ignoreCase=_this.addOption(function(j){
			return j.IgnoreCase==true;
		},"IgnoreCase");
		_this._to=_this.addOption(function(j){
			return j.To;
		},"To");
		_this._keepCase=_this.addOption(function(j){
			return j.KeepCase!=false;
		},"KeepCase");
		_this.loadJson(data);
		return _this;
	}

	HotStringReplace.prototype.updateControls=function(){
		_super.prototype.updateControls.call(this);
		if(this._from.value.length==this._to.value.length){
			this._keepCase.removeAttribute("disabled");
		}else{
			this._keepCase.setAttribute("disabled","true");
			this._keepCase.checked=false;
		}
		if(this._keepCase.checked){
			this._ignoreCase.setAttribute("disabled","true");
			this._ignoreCase.checked=true;
		}else
			this._ignoreCase.removeAttribute("disabled");
	};
	HotStringReplace.prototype.toJson=function(){
		return {
			From:this._from.value,
			IgnoreCase:this._ignoreCase.checked||undefined,
			To:this._to.value,
			KeepCase:this._from.value.length==this._to.value.length&& !this._keepCase.checked?false:undefined
		};
	};
	HotStringReplace.prototype.getFromTo=function(){
		return ["Replace",this._from.value,this._to.value];
	};
	return HotStringReplace;
}(HotString));
//endregion
//region Key Combos
function initKeyCombos(){
	var keycombos=document.getElementsByClassName("keycombo");
	for(var i=0; i<keycombos.length; i++){
		var keycombo=keycombos[i];
		setKeyCombo(keycombo,keycombo.textContent);
	}
}

function setKeyCombo(element,keycombo){
	var fragment=document.createDocumentFragment();
	var first=true;
	for(var _i=0,_a=keycombo.split(/ *\| */); _i<_a.length; _i++){
		var combo=_a[_i];
		if(first)
			first=false;
		else
			fragment.appendChild(document.createTextNode(" | "));
		for(var _b=0,_c=combo.split(/ *\+ */); _b<_c.length; _b++){
			var key=_c[_b];
			var keyElement=document.createElement("span");
			keyElement.classList.add("key");
			keyElement.textContent=key;
			fragment.appendChild(keyElement);
		}
	}
	while(element.firstChild)
		element.removeChild(element.lastChild);
	element.appendChild(fragment);
}

//endregion
//region On Change Listener
function updateElement(target){
	if("rows" in target){
		target.rows=1;
		var parent_1=target.parentElement;
		var preStyle=parent_1.getAttribute("style");
		parent_1.style.height=parent_1.clientHeight+"px";
		target.style.height="auto";
		target.style.height=target.scrollHeight+"px";
		if(preStyle)
			parent_1.setAttribute("style",preStyle);
		else
			parent_1.removeAttribute("style");
	}

	function applyAnyNumber(){
		if(target.value.length==0){
			target.value="0";
			target.setSelectionRange(1,1);
		}else
			while(target.value.length!=1&&(target.value[0]=="0"||target.value[0]=="-")){
				var oldSelectionStart=target.selectionStart-1;
				var oldSelectionEnd=target.selectionEnd-1;
				target.value=target.value.substring(1);
				target.setSelectionRange(oldSelectionStart,oldSelectionEnd);
			}
		//FIXME broken caret backtracing, if moving caret in between inputs
	}

	if(target.classList.contains("long")){
		applyAnyNumber();
		if((+target.value)+""===target.value&&/^\d+$/.test(target.value)) //if string representation of converted is same
			target.old=[target.value,target.selectionStart,target.selectionEnd];
		else{
			var _a=target.old,_b=_a[0],value=_b=== void 0?"":_b,start=_a[1],end=_a[2];
			target.value=value;
			target.setSelectionRange(start,end);
		}
	}else if(target.classList.contains("int")){
		applyAnyNumber();
		if((+target.value|0)+""===target.value) //if string representation of converted is same ("|0" is used to clamp to int)
			target.old=[target.value,target.selectionStart,target.selectionEnd];
		else{
			var _c=target.old,_d=_c[0],value=_d=== void 0?"":_d,start=_c[1],end=_c[2];
			target.value=value;
			target.setSelectionRange(start,end);
		}
	}else if(target.classList.contains("byte")){
		applyAnyNumber();
		if(/^-?([1-9]?\d|1\d\d|2[0-4]\d|25[0-5])$/.test(target.value))
			target.old=[target.value,target.selectionStart,target.selectionEnd];
		else{
			var _e=target.old,_f=_e[0],value=_f=== void 0?"":_f,start=_e[1],end=_e[2];
			target.value=value;
			target.setSelectionRange(start,end);
		}
	}
	target.classList.remove("error");
	target.removeAttribute("title");
	return false;
}

function initChangeListener(){
	var named=document.querySelectorAll("input[name],textarea[name],.check.box[name]");
	for(var i=0; i<named.length; i++){
		var element=named[i];
		element.addEventListener("input",onInput);
		updateElement(element);
	}
	var now=document.querySelectorAll(".now");
	for(var i=0; i<now.length; i++){
		var element=now[i];
		element.addEventListener("input",onInput);
		updateElement(element);
	}
}

function onInput(evt){
	var target=evt.target;
	var b=updateElement(target);
	if(b){
		evt.preventDefault();
		evt.stopPropagation();
	}
	if(target.name.indexOf("=")!= -1){
		send(target.name);
		return;
	}
	var value=target.type=="checkbox"?target.checked:target.value;
	var key=target.classList.contains("now")?target.id:target.name;
	send(key+"="+JSON.stringify(value));
}

function setupCheckbox(checkbox,classElement,clazz){
	if(classElement=== void 0){
		classElement=null;
	}
	if(clazz=== void 0){
		clazz="checked";
	}
	checkbox.tabIndex=0;
	if(classElement==null){
		if(!checkbox.hasAttribute("name")||checkbox.getAttribute("name").indexOf("=")== -1)
			checkbox.classList.add("check");
		classElement=checkbox;
	}
	checkbox.classList.add("box");
	checkbox.type="checkbox";
	Object.defineProperty(checkbox,"name",{
		get:function(){
			return checkbox.getAttribute("name");
		},
		set:function(v){
			if(v!=undefined)
				checkbox.setAttribute("name",v);
			else
				checkbox.removeAttribute("name");
		}
	});
	Object.defineProperty(checkbox,"checked",{
		get:function(){
			return classElement.classList.contains(clazz);
		},
		set:function(v){
			if(v)
				classElement.classList.add(clazz);
			else
				classElement.classList.remove(clazz);
		}
	});
	Object.defineProperty(checkbox,"disabled",{
		get:function(){
			return checkbox.hasAttribute("disabled");
		},
		set:function(v){
			if(v)
				checkbox.setAttribute("disabled","true");
			else
				checkbox.removeAttribute("disabled");
		}
	});

	function toggle(){
		classElement.classList.toggle(clazz);
		var event;
		if(typeof Event==="function")
			event=new Event("input");
		else{
			event=document.createEvent("Event");
			event.initEvent("input",false,false);
		}
		checkbox.dispatchEvent(event);
		if((checkbox.name||"").indexOf("=")!= -1)
			receive(checkbox.name);
	}

	checkbox.addEventListener("click",function(){
		if(checkbox.hasAttribute("disabled"))
			return;
		checkbox.focus();
		toggle();
	});
	checkbox.addEventListener("keypress",function(e){
		if([" ","Space","Spacebar"].indexOf(e.key)== -1)
			return;
		e.preventDefault();
		toggle();
	});
}

//endregion
//# sourceMappingURL=script.js.map