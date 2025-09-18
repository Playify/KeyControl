//region Dark Mode
// noinspection JSUnusedGlobalSymbols
function setDarkMode(b:boolean){
	localStorage&&localStorage.setItem("dark",""+b);
	document.documentElement.classList[b?"add":"remove"]("dark");
}

{
	const storageDark=localStorage&&localStorage.getItem("dark");
	let wantsDark=storageDark=="true"||storageDark=="false"?storageDark=="true":window.matchMedia('(prefers-color-scheme: dark)').matches;
	document.documentElement.classList[wantsDark?"add":"remove"]("dark");
}

//endregion

interface OwnExternal{
	Hash:string;
	Available:boolean;

	Log(d:any):void;

	Init(d:(s:string)=>void):void;

	send(s:string):void;
}

//@ts-ignore
// noinspection JSDeprecatedSymbols
const windowExternal=(window as any)["external"];
let ownExternal:OwnExternal | null=windowExternal&&windowExternal.Available?windowExternal:null;


//region send&receive
const pending:string[]=[];
let ws:WebSocket;

send(null);


function send(data:any | null):void{
	if(data!=null) pending.push(typeof data=="string"?data:JSON.stringify(data));

	if(ws&&(ws==ownExternal as any||ws.readyState==WebSocket.OPEN)){
		if(!pending.length) return;
		ws.send(pending.shift()!);
		setTimeout(send,0);
	}else if(!ws||ws.readyState==WebSocket.CLOSING||ws.readyState==WebSocket.CLOSED){
		if(ownExternal){
			ws=ownExternal as any;
			ownExternal.Init(s=>receive(JSON.parse(s)));
			setTimeout(send,0);
			return;
		}

		const url="ws"+document.location.origin.substring(4);
		ws=new WebSocket(url);
		ws.onclose=function(){
			ws.close();
			setTimeout(send,100);
		};
		ws.onerror=function(){
			ws.close();
			setTimeout(send,100);
		};
		ws.onmessage=function(e){
			receive(JSON.parse(e.data));
		};
		ws.onopen=function(){
			setTimeout(send,0);
		};
	}
}

function receive(json:any){
	if(Array.isArray(json)){
		const value=json.pop();

		let obj=registered;
		for(let key of json) obj=obj&&obj[key];
		if(!obj) json.push(value);
		else{
			//console.log("Received:",json,value);
			obj(value);
			return;
		}
	}else{
		//console.log("HotString:",json);
		updateHotString(json);
		return;
	}
	console.warn("Received unknown: ",json);
}

const registered:any={};

function register(keys:string[],func:(value:any)=>void){
	let obj=registered;
	const last=keys.pop()!;
	for(let key of keys) obj=(obj[key]||={});
	obj[last]=func;
}

//endregion

//region Navigation
document.addEventListener("DOMContentLoaded",function initNavigation(){
	if(ownExternal){
		for(const a of document.querySelectorAll("nav>a") as any as HTMLAnchorElement[]){
			const href=a.getAttribute("href");
			a.onclick=e=>{
				ownExternal!.Hash=href!;
				e.preventDefault();
				onHashChange();
			}
		}
	}
	onHashChange();
});
window.onhashchange=onHashChange;

function onHashChange(){
	const hash=ownExternal?ownExternal.Hash:document.location.hash;
	const defaultHash="#windows";
	let found=hash==defaultHash;

	for(const section of document.querySelectorAll("main>*") as any as HTMLElement[])
		if(("#"+section.id)==hash){
			section.classList.add("active");
			found=true;
		}else section.classList.remove("active");

	if(!found){
		if(ownExternal) ownExternal.Hash=defaultHash;
		else document.location.hash=defaultHash;
		return onHashChange();
	}

	for(const a of document.querySelectorAll("nav>a") as any as HTMLAnchorElement[])
		if(a.getAttribute("href")==hash) a.classList.add("active");
		else a.classList.remove("active");
}

//endregion

//region CheckBoxes
document.addEventListener("DOMContentLoaded",function initCheckboxes(){
	for(const checkbox of document.querySelectorAll(".checkbox") as any as HTMLElement[])
		setupCheckbox(checkbox);
});

function setupCheckbox(checkbox:HTMLElement):void;
function setupCheckbox(checkbox:HTMLElement,onToggle:(b:boolean)=>void):void;
function setupCheckbox(checkbox:HTMLElement,onToggle:()=>void,asButton:true):void;
function setupCheckbox(checkbox:HTMLElement,onToggle?:(b:boolean)=>void,asButton:boolean=false):void{
	checkbox.tabIndex=0;

	checkbox.addEventListener("click",()=>{
		checkbox.focus();
		toggle();
	});
	checkbox.addEventListener("keypress",e=>{
		if([" ","Space","Spacebar"].indexOf(e.key)== -1) return;
		e.preventDefault();
		toggle();
	});
	const name=checkbox.getAttribute("name");
	if(name) register(name.split('.'),b=>{
		checkbox.classList[b?"add":"remove"]("checked");
		onToggle&&onToggle(b);
	});

	function toggle(){
		if(name){
			const keys:any[]=name.split('.');
			keys.push(!checkbox.classList.contains("checked"));
			send(JSON.stringify(keys));
		}else if(asButton){
			onToggle&&onToggle(false);
		}else{
			const b=checkbox.classList.toggle("checked");
			onToggle&&onToggle(b);
		}
	}
}

//endregion

//region KeyCombos
document.addEventListener("DOMContentLoaded",function initKeyCombos(){
	for(const element of document.querySelectorAll(".keycombo") as any as HTMLElement[]){

		const fragment=document.createDocumentFragment();
		let first=true;
		for(let combo of element.textContent!.split(/ *\| */)){
			if(first) first=false;
			else fragment.appendChild(document.createTextNode(" | "));

			for(let key of combo.split(/ *\+ */)){

				const keyElement=document.createElement("span");
				keyElement.classList.add("key");
				keyElement.textContent=key;
				fragment.appendChild(keyElement);
			}
		}
		while(element.firstChild) element.removeChild(element.lastChild!);
		element.appendChild(fragment);
	}
});
//endregion

//region RotateWASD
document.addEventListener("DOMContentLoaded",()=>{
	const button=document.getElementById("rotateWasd")!;
	button.onclick=()=>send(["Games","Wasd",0]);

	for(let checkbox of (document.querySelectorAll(".rotateWasd .box") as any as HTMLElement[])){
		setupCheckbox(checkbox,()=>{
			send(["Games","Wasd",+checkbox.getAttribute("value")!])
		},true);
	}

	register(["Games","Wasd"],i=>{
		for(let checked of document.querySelectorAll(".rotateWasd .checked") as any as HTMLElement[])
			checked.classList.remove("checked");
		document.querySelector(".rotateWasd .box[value='"+i+"']")!.classList.add("checked");
		button.style.visibility=i==0?"hidden":"visible";
	});
});
//endregion

//region TextBoxes
document.addEventListener("DOMContentLoaded",()=>{

	//Textarea autoheight
	for(let textarea of (document.querySelectorAll("textarea") as any as HTMLTextAreaElement[]))
		initTextArea(textarea);

	for(let input of (document.querySelectorAll("input[name],textarea[name]") as any as (HTMLInputElement | HTMLTextAreaElement)[])){
		let systemValue:string | null=null;//If never received a value, then don't override on blur

		register(input.getAttribute("name")!.split('.'),v=>{
			systemValue=input.classList.contains("color")?(v as number).toString(16).toUpperCase():v.toString();

			if(document.activeElement==input) return;//Don't override while focused

			input.value=systemValue!;
			// @ts-ignore
			if("autoSize" in input) input.autoSize();
		});
		const sendValue=(v:any)=>{
			const keys=input.getAttribute("name")!.split('.');
			keys.push(v);
			send(keys);
		}
		const inputFunction=function(){
			{//Restrict letters using regex
				let value=input.value;
				let start=input.selectionStart!;
				let end=input.selectionEnd!;
				let direction=input.selectionDirection!;
				value=value.substring(0,start)+'\0'+value.substring(start,end)+'\0'+value.substring(end);

				if(input.classList.contains("color"))
					value=value.toUpperCase().replace(/[^0-9A-F\x00]/,'');
				else if(input.classList.contains("int")){
					value=value.replace(/[^0-9\x00-]/,'');
					const m=value.match(/^(\x00*-?)(.*)/)!;
					value=m[1]+m[2].replace(/-/g,"");
				}


				start=value.indexOf('\0');
				end=value.lastIndexOf('\0')-1;
				input.value=value.replace(/\x00/g,'');
				if(document.activeElement==input)
					input.setSelectionRange(start,end,direction);
			}


			let error=false;
			if(input.classList.contains("color")){
				if(/^[0-9A-Fa-f]{1,8}$/.test(input.value)) sendValue(parseInt(input.value,16));
				else error=true;
			}else if(input.classList.contains("int")){
				if(/^-?[0-9]+$/.test(input.value)){
					const number=+input.value;
					const min=+(input.getAttribute("min")|| -0x7FFFFFFF);
					const max=+(input.getAttribute("max")||0x7FFFFFFF);
					if(number>=min&&number<=max) sendValue(number);
					else error=true;
				}else error=true;
			}else{
				sendValue(input.value);
			}

			input.classList[error?"add":"remove"]("error");
		};
		input.addEventListener("input",inputFunction);
		input.addEventListener("blur",()=>{
			inputFunction();
			if(systemValue!=null&&input.value!=systemValue)
				input.value=systemValue;
			input.classList.remove("error");
			// @ts-ignore
			if("autoSize" in input) input.autoSize();
		});
	}
});

function initTextArea(textarea:HTMLTextAreaElement){
	if(textarea.parentElement!.id=="test") return;

	function autoSize(){
		textarea.rows=1;
		const parent=textarea.parentElement!;
		const preStyle=parent.getAttribute("style");
		parent.style.height=parent.clientHeight+"px";
		textarea.style.height="auto";
		textarea.style.height=textarea.scrollHeight+"px";
		if(preStyle) parent.setAttribute("style",preStyle);
		else parent.removeAttribute("style");
	}

	textarea.addEventListener("input",autoSize);
	// @ts-ignore
	textarea.autoSize=autoSize;
	autoSize();
}

//endregion

//region HotStrings
const map:HotString[]=[];
const blocked:HotString[]=[];
let root:HotStringCategory;
document.addEventListener("DOMContentLoaded",()=>{
	root=new HotStringCategory(null,null);
	register(["HotStrings","List"],j=>{
		for(let i=root.childs.length-1; i>=0; i--) root.childs[i].destroy(true);
		for(let child of j) root.addChild(getHotString(root,child));
	});
	register(["Internal","HotStrings","Block"],j=>{
		for(let hotString of blocked) hotString.div.classList.remove("blocked");
		blocked.length=0;
		for(let i of j){
			const hotString=getHotString(null,i);
			blocked.push(hotString);
			hotString.div.classList.add("blocked");
		}
	});
});

function getHotString(parent:HotStringCategory | null,child:any):HotString{
	if(typeof child=="number") return map[child];
	if("Category" in child) return new HotStringCategory(parent,child);
	if("Emoji" in child) return new HotStringEmoji(parent,child);
	if("Regex" in child) return new HotStringRegex(parent,child);
	return new HotStringReplace(parent,child);
}

function updateHotString(value:any):HotString{
	const hotString=map[value.Id];
	if(!hotString) return getHotString(null,value);
	hotString.loadJson(value);
	return hotString;
}


function makeDragable(hs:HotString){
	const element=hs.div;
	element.classList.add("drag");

	function reposition(e:MouseEvent):void{
		const posY=e.clientY;
		let nearest=Infinity;
		let inserter:(null | ((hs:HotString)=>void))=null;

		root.forEach(hs,(y,ins)=>{
			y=Math.abs(posY-y);
			if(y<nearest){
				nearest=y;
				inserter=ins;
			}
		});

		//nearest element is itself. it's easier to check for null than to check inserter variable
		if(inserter==null) return;

		const prevParent=hs.parent!;
		const prevChilds=prevParent.childs.slice();
		(inserter as any)(hs);
		if(hs.parent!=prevParent){
			hs.parent!.send();
			return;
		}
		const afterChilds=hs.parent!.childs;
		if(prevChilds.length!=afterChilds.length){
			hs.parent!.send();
			return;
		}
		for(let i=0; i<afterChilds.length; i++)
			if(prevChilds[i]!=afterChilds[i]){
				hs.parent!.send();
				return;
			}
	}

	element.addEventListener("mousedown",function(e){
		if(e.target!=element) return;
		e.preventDefault();
		e.stopPropagation();
		reposition(e);
		//element.focus();
		(document.activeElement as HTMLElement)?.blur?.();
		element.classList.add("dragging");


		document.addEventListener("mousemove",reposition);

		function onMouseUp(e:MouseEvent){
			reposition(e);
			element.classList.remove("dragging");
			document.removeEventListener("mousemove",reposition);
			document.removeEventListener("mouseup",onMouseUp);
		}

		document.addEventListener("mouseup",onMouseUp);
	});
}

abstract class HotString{
	public readonly id:number;
	public parent:HotStringCategory | null=null;
	public readonly div:HTMLDivElement;
	public readonly titleText:HTMLInputElement | HTMLSpanElement;
	private readonly options:[HTMLElement,(json:any)=>(string | boolean)][]=[];
	private readonly errorBox:HTMLElement;

	protected constructor(parent:HotStringCategory | null,data:any){
		if(data==null){
			this.id=0;
			this.div=document.querySelector<HTMLDivElement>("#hotstrings")!;
			return;
		}
		this.id=data.Id;
		map[this.id]=this;
		const isCategory="Category" in data;

		this.div=document.createElement("div");
		this.div.classList.add(isCategory?"category":"hotstringContainer");

		makeDragable(this);

		{//Titlebar
			const title=document.createElement("div");
			this.div.appendChild(title).classList.add("title");


			const collapsed=document.createElement("div");
			collapsed.classList.add("collapsed");
			collapsed.classList.add("box");
			setupCheckbox(collapsed,()=>{
				this.div.classList.toggle("collapsed");
				this.send();
			},true);
			if(data.Collapsed!=false) this.div.classList.add("collapsed");
			title.appendChild(collapsed);


			const enabled=document.createElement("div");
			enabled.classList.add("enabled");
			enabled.classList.add("box");
			setupCheckbox(enabled,()=>{
				this.div.classList.toggle("enabled");
				this.send();
			},true);
			if(data.Enabled!=false) this.div.classList.add("enabled");
			title.appendChild(enabled);

			title.appendChild(this.errorBox=document.createElement("abbr")).textContent="[ERROR]";


			this.titleText=document.createElement(isCategory?"input":"span");
			title.appendChild(this.titleText);

			if(!isCategory){
				this.titleText.appendChild(document.createElement("span"));//Type
				this.titleText.appendChild(document.createTextNode(": "));
				const from=this.titleText.appendChild(document.createElement("span"));
				from.classList.add("hotstring");
				from.classList.add("from");
				this.titleText.appendChild(document.createTextNode(" ⇒ "));
				const to=this.titleText.appendChild(document.createElement("span"));
				to.classList.add("hotstring");
				to.classList.add("to");
			}

			const trash=document.createElement("div");
			trash.classList.add("trash");
			trash.classList.add("box");
			setupCheckbox(trash,b=>{
				if(b) setTimeout(()=>trash.classList.remove("checked"),1000);
				else{
					const parent=this.parent;
					this.destroy(true);
					parent&&parent.send();
					this.send();
				}
			});

			trash.appendChild(document.createElement("div"));
			title.appendChild(trash);
		}

		if(parent) parent.addChild(this);
	}

	forEach(hs:HotString,func:(y:number,inserter:null | ((hs:HotString)=>void))=>void){
		func(this.div.getBoundingClientRect().top,this==hs?null:this.addBefore.bind(this));
	}

	addBefore(hs:HotString){
		hs.destroy(false);
		if(!this.parent) return;
		const i=this.parent.childs.indexOf(this);
		if(i!= -1) this.parent.childs.splice(i,0,hs);
		hs.parent=this.parent;
		this.parent.div.insertBefore(hs.div,this.div);
	}

	destroy(removeHtml:boolean){
		if(this.parent==null) return;
		const i=this.parent.childs.indexOf(this);
		if(i!= -1) this.parent.childs.splice(i,1);

		if(removeHtml) this.div.parentElement?.removeChild(this.div);
		this.parent=null;
	}

	send(){
		this.updateControls();
		if(this.parent==null){
			const id=this.id;
			delete map[id];
			send(id);
		}else{
			const json:any=this.toJson();
			json.Enabled=this.div.classList.contains("enabled")&&undefined;
			json.Collapsed=this.div.classList.contains("collapsed")&&undefined;
			json.Id=this.id;

			send(json);
		}
	}

	loadJson(json:any){
		for(let [input,func] of this.options){
			const value=func(json);
			if(typeof value=="boolean") input.classList[value?"add":"remove"]("checked");
			else (input as HTMLInputElement).value=value||"";
		}
		this.div.classList[json.Enabled!=false?"add":"remove"]("enabled");
		this.div.classList[json.Collapsed!=false?"add":"remove"]("collapsed");

		if(this.errorBox){
			if(json.Error) this.errorBox.title=json.Error;
			else this.errorBox.removeAttribute("title");
		}

		this.updateControls();
	}


	abstract toJson():object;

	abstract getFromTo():[string,string,string];

	protected updateControls(){
		const arr=this.getFromTo();
		this.titleText.children[0].textContent=arr[0];
		this.titleText.querySelector(".from")!.textContent=arr[1];
		this.titleText.querySelector(".to")!.textContent=arr[2];
	}

	protected addOption(func:(json:any)=>string,name:string,sub?:string):HTMLInputElement;
	protected addOption(func:(json:any)=>boolean,name:string,sub?:string):HTMLDivElement;
	protected addOption(func:(json:any)=>(string | boolean),name:string,sub?:string):HTMLElement{
		const row=this.div.appendChild(document.createElement("tr"));

		const nameEleemnt=row.appendChild(document.createElement("td"));
		nameEleemnt.textContent=name;
		if(sub) nameEleemnt.appendChild(document.createElement("sub")).textContent=sub;

		let input:HTMLElement;
		if(typeof (func({}))=="boolean"){
			input=document.createElement("div");
			input.classList.add("checkbox");
			setupCheckbox(input,()=>{
				this.updateControls();
				this.send();
			});
		}else{
			input=document.createElement("input");
			input.addEventListener("blur",()=>send(["Internal","HotStrings","Block",0]));
			input.addEventListener("focus",()=>send(["Internal","HotStrings","Block",this.id]));

			input.addEventListener("input",()=>{
				this.updateControls();
				this.send();
			});
		}
		row.appendChild(document.createElement("td")).appendChild(input);
		this.options.push([input,func]);

		return input;
	}
}

class HotStringCategory extends HotString{
	public readonly titleText:HTMLInputElement;
	public readonly createNew:HTMLDivElement;
	public readonly childs:HotString[]=[];

	constructor(parent:HotStringCategory | null,data:any){
		super(parent,data);

		if(data==null){
			this.addBefore=()=>{
			};
		}else{
			this.titleText.value=data.Category;
			this.titleText.addEventListener("input",()=>this.send());//TODO maybe fix: will be overridden even when focused
		}

		for(let child of ((data&&data.Children)||[]))
			this.addChild(getHotString(this,child));

		this.div.appendChild(this.createNew=document.createElement("div")).classList.add("createNew");

		const addOption=(name:string,create:()=>HotString)=>{
			const div=this.createNew.appendChild(document.createElement("div"));
			div.textContent=name;
			div.addEventListener("click",()=>{
				create().send();
				this.send();
			});
		}


		addOption("Category",()=>new HotStringCategory(this,{
			Id:Date.now(),
			Category:"Category",
			Collapsed:false,
		}));
		addOption("Emoji",()=>new HotStringEmoji(this,{
			Id:Date.now(),
			From:"xdd",
			Emoji:"😂",
			Collapsed:false,
		}));
		addOption("Regex",()=>new HotStringRegex(this,{
			Id:Date.now(),
			Regex:"(?<=^| )itn$",
			Replacement:"int",
			IgnoreCase:false,
			Collapsed:false,
		}));
		addOption("Replace",()=>new HotStringReplace(this,{
			Id:Date.now(),
			From:"cosnt",
			To:"const",
			IgnoreCase:false,
			Collapsed:false,
		}));
	}

	forEach(hs:HotString,func:(y:number,inserter:(((hs:HotString)=>void) | null))=>void){
		if(this==hs){
			if(this.parent!=null) func(this.div.getBoundingClientRect().top,null);
			return;
		}
		if(this.parent) func(this.div.getBoundingClientRect().top,this.addBefore.bind(this));

		if(this.div.classList.contains("collapsed")) return;

		for(let child of this.childs) child.forEach(hs,func);

		func(this.createNew.getBoundingClientRect().top,this.addChild.bind(this));
	}

	addChild(hs:HotString){
		hs.destroy(false);
		this.childs.push(hs);
		hs.parent=this;
		this.div.insertBefore(hs.div,this.createNew);
	}

	send(){
		if(this.id==0) return send(["HotStrings","List",this.childs.map(c=>c.id)]);
		if(this.parent==null){
			//freeing children
			for(let i=this.childs.length-1; i>=0; i--){
				const child=this.childs[i];
				child.destroy(true);
				child.send();
			}
		}
		super.send();
	}

	loadJson(json:any){
		super.loadJson(json);

		this.titleText.value=json.Category;
		const arr=json.Children||[];
		for(let i=this.childs.length-1; i>=0; i--)
			this.childs[i].destroy(arr.indexOf(this.childs[i].id)== -1);//only remove html if needed
		for(let child of arr)
			this.addChild(getHotString(null,child));
	}

	toJson():object{
		return {
			Category:this.titleText.value,
			Children:this.childs.map(c=>c.id),
		};
	}

	getFromTo():[string,string,string]{
		return ["","",""];
	}

	protected updateControls(){
	}
}

class HotStringEmoji extends HotString{
	private readonly _from:HTMLInputElement;
	private readonly _regex:HTMLInputElement;
	private readonly _ignoreCase:HTMLDivElement;
	private readonly _emoji:HTMLInputElement;

	constructor(parent:HotStringCategory | null,data:any){
		super(parent,data);
		this._from=this.addOption(j=>j.From,"From");
		this._regex=this.addOption(j=>j.Regex,"Regex","(optional)");
		this._ignoreCase=this.addOption(j=>j.IgnoreCase==true,"IgnoreCase");
		this._emoji=this.addOption(j=>j.Emoji,"Emoji");
		this._emoji.classList.add("emoji");

		this.loadJson(data);
	}

	toJson():any{
		return {
			From:this._from.value,
			Emoji:this._emoji.value,
			Regex:this._regex.value||undefined,//use value, but if empty string then dont send anything
			IgnoreCase:this._ignoreCase.classList.contains("checked")||undefined
		}
	}

	getFromTo():[string,string,string]{
		return ["Emoji",this._from.value,this._emoji.value];
	}
}

class HotStringRegex extends HotString{
	private readonly _replacement:HTMLInputElement;
	private readonly _regex:HTMLInputElement;
	private readonly _ignoreCase:HTMLDivElement;

	constructor(parent:HotStringCategory | null,data:any){
		super(parent,data);
		this._regex=this.addOption(j=>j.Regex,"Regex");
		this._ignoreCase=this.addOption(j=>j.IgnoreCase==true,"IgnoreCase");
		this._replacement=this.addOption(j=>j.Replacement,"Replacement");

		this.loadJson(data);
	}

	toJson():any{
		return {
			Regex:this._regex.value,
			IgnoreCase:this._ignoreCase.classList.contains("checked"),
			Replacement:this._replacement.value
		}
	}

	getFromTo():[string,string,string]{
		return ["Regex",this._regex.value,this._replacement.value];
	}
}

class HotStringReplace extends HotString{
	private readonly _from:HTMLInputElement;
	private readonly _ignoreCase:HTMLDivElement;
	private readonly _to:HTMLInputElement;
	private readonly _keepCase:HTMLDivElement;

	constructor(parent:HotStringCategory | null,data:any){
		super(parent,data);
		this._from=this.addOption(j=>j.From,"From");
		this._ignoreCase=this.addOption(j=>j.IgnoreCase==true,"IgnoreCase");
		this._to=this.addOption(j=>j.To,"To");
		this._keepCase=this.addOption(j=>j.KeepCase!=false,"KeepCase");

		this.loadJson(data);
	}

	updateControls():void{
		super.updateControls();
		if(this._from.value.length==this._to.value.length){
			this._keepCase.removeAttribute("disabled");
		}else{
			this._keepCase.setAttribute("disabled","true");
			this._keepCase.classList.remove("checked");
		}
		if(this._keepCase.classList.contains("checked")){
			this._ignoreCase.setAttribute("disabled","true");
			this._keepCase.classList.add("checked");
		}else this._ignoreCase.removeAttribute("disabled");
	}

	toJson():any{
		return {
			From:this._from.value,
			IgnoreCase:this._ignoreCase.classList.contains("checked")||undefined,
			To:this._to.value,
			KeepCase:this._from.value.length==this._to.value.length&& !this._keepCase.classList.contains("checked")?false:undefined
		}
	}

	getFromTo():[string,string,string]{
		return ["Replace",this._from.value,this._to.value];
	}
}

//endregion

//region Test Area
document.addEventListener("DOMContentLoaded",()=>{
	const textarea=document.querySelector<HTMLTextAreaElement>("#test>textarea")!;
	const div=document.querySelector("#test>div")!;
	const input=()=>{
		const content=textarea.value;

		//replace surrogate pairs with single char
		const contentLength=content.replace(/[\uD800-\uDBFF][\uDC00-\uDFFF]/g,'_').length;

		div.textContent=
			`Length: ${contentLength}\n`+
			`UTF-16: ${content.length}\n`+
			`Lines: ${content.split(/\r\n|\r|\n/).length}`;
	};
	textarea.addEventListener("input",input);
	input();
});
//endregion