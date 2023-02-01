//region Dark Mode
// noinspection JSUnusedGlobalSymbols
function setDarkMode(b: boolean){
	localStorage?.setItem("dark",""+b);
	if(b) document.documentElement.classList.add("dark");
	else document.documentElement.classList.remove("dark");
	return true;
}

{
	let wantsDark: boolean;
	const storageDark=localStorage?.getItem("dark");
	if(storageDark=="true"||storageDark=="false") wantsDark=storageDark=="true";
	else wantsDark=window.matchMedia('(prefers-color-scheme: dark)').matches;

	//don't add event listener, because C# Form also doesn't respond to changes

	if(wantsDark) document.documentElement.classList.add("dark");
	else document.documentElement.classList.remove("dark");
}
//endregion

//region External
interface Sender{
	Send(s: string): void;
}

interface OwnExternal{
	Hash: string;

	Log(s: string): void;

	Close();

	Init(recv: (s: string)=>void): Sender;
}

// noinspection JSDeprecatedSymbols
const ownExternal: OwnExternal=(<any>external)?.Available?<any>external: null;
//endregion


//region Hash
function initNavigation(){
	if(!ownExternal) return;
	const as: NodeListOf<HTMLAnchorElement>=document.querySelectorAll("nav>a");
	for(let i=0; i<as.length; i++){
		const a=as[i];
		//remove href to avoid creation of history
		const href=a.getAttribute("href");/*
		a.removeAttribute("href");
		a.tabIndex=0;*/

		a.onclick=e=>{
			ownExternal.Hash=href;
			e.preventDefault();

			onHashChange();
		}
	}
}

function onHashChange(){
	const defaultHash="#hotkeys";
	const hash=ownExternal?.Hash??document.location.hash;
	const sections=document.querySelectorAll("main>*");
	let found=hash==defaultHash;
	for(let i=0; i<sections.length; i++){
		const section=sections[i];
		const b=("#"+section.id)==hash;
		if(b) section.classList.add("active");
		else section.classList.remove("active");
		if(b) found=true;
	}
	if(!found){
		if(ownExternal) ownExternal.Hash=defaultHash;
		else document.location.hash=defaultHash;
		onHashChange();
		return;
	}
	const as: NodeListOf<HTMLAnchorElement>=document.querySelectorAll("nav>a");
	for(let i=0; i<as.length; i++){
		const a=as.item(i);
		if(a.getAttribute("href")==hash) a.classList.add("active");
		else a.classList.remove("active");
	}
}

window.onhashchange=onHashChange;

//endregion

function testInfo(){
	const testA=document.querySelector("nav>a[href=\"#test\"] sub");
	const test=<HTMLTextAreaElement>document.getElementById("test");
	test.focus();

	const content=test.value;

	//replace surrogate pairs with single char
	const contentLength=content.replace(/[\uD800-\uDBFF][\uDC00-\uDFFF]/g,'_').length;

	const text=
		`Length: ${contentLength} (UTF-16: ${content.length})\n`+
		`Lines: ${content.split(/\r\n|\r|\n/).length}`;

	testA.setAttribute("title",text);
}

//region WebSocket
let loaded=false;

const pending: string[]=[];
let ws=null;
let _send: Sender;
if(ownExternal) _send=ownExternal.Init(receive);
else send(null);

function send(s: string): void{
	if(s!=null&&_send!=null){
		_send.Send(s);
		return;
	}
	if(s!=null) pending.push(s);

	if(ws&&ws.readyState==WebSocket.OPEN){
		if(!pending.length) return;
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

		const url="ws"+new URL(document.URL).origin.substring(4);
		ws=new WebSocket(url);
		ws.onclose=function(){
			ws.close();
			setTimeout(send,100);
		};
		ws.onerror=function(){
			ownExternal?.Log("Error connecting to Websocket: "+url);
			ws.close();
			setTimeout(send,100);
		};
		ws.onmessage=receive;
		ws.onopen=function(){
			setTimeout(send,0);
		};
	}
}

function receive(e: MessageEvent | string): void{
	const s: string=typeof e=="string"?e: e.data;
	if(typeof s!=="string"){
		console.error("Error reading WebSocket Data: ",s);
		return;
	}
	console.log("RECV:"+s);
	const indexOf=s.indexOf('=');
	const key=s.substring(0,indexOf);
	const valueStr=s.substring(indexOf+1);
	const value=JSON.parse(valueStr);

	if(key=="Random"){
		randomID=value;
	}else if(key=="hotString"){
		HotString.update(value);
	}else if(key=="hotStringBlock"){
		HotString.updateBlocked(value);
	}else if(key.indexOf("err:")==0){
		const elements=document.getElementsByName(key.substring(4));
		for(let i=0; i<elements.length; i++){
			const element=<HTMLInputElement>elements[i];
			element.classList.add("error");
			element.title=value;
		}
	}else{
		const elements=document.getElementsByName(key);

		const startsWith=document.querySelectorAll("[name^=\""+key+"=\"]");
		if(elements.length==0&&startsWith.length==0){
			console.error("Unknown Key: "+key);
			return;
		}
		for(let i=0; i<startsWith.length; i++){
			const element=<HTMLInputElement>startsWith[i];
			const elementValue=element.getAttribute("name").substring(key.length+1);
			if(element.type=="checkbox") element.checked=elementValue==valueStr;
			else element.value=elementValue==valueStr?value: "";
			updateElement(element);
		}
		for(let i=0; i<elements.length; i++){
			const element=<HTMLInputElement>elements[i];
			if(element.type=="checkbox") element.checked=value;
			else element.value=value;
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

	const checkboxes=document.querySelectorAll(".check.box,.box[name*=\"=\"]");
	for(let i=0; i<checkboxes.length; i++)
		setupCheckbox(<HTMLInputElement>checkboxes[i]);

	onHashChange();

	initKeyCombos();

	initChangeListener();


	Category.master;//enforce loading of Master
});

//region Rainbow
{
	let curr="";
	const konami="uuddlrlrba";
	document.addEventListener("keydown",function(e){
		let k: string;
		switch(e.key){
			case "Escape":
			case "Esc":
				ownExternal?.Close();
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
				break;//handled by other
			default:
				k="";
				break;
		}
		if(!k) curr="";
		curr+=k;
		if(curr.length>konami.length) curr=curr.substring(curr.length-konami.length);
		if(curr==konami) document.documentElement.classList.toggle("rainbow");
	});
}
//endregion

//region HotStrings

let randomID=Math.floor(Math.random()*(-1>>>0));//Number.MAX_SAFE_INTEGER


function getRandomId(){
	while(HotString.IdMap[++randomID]){
	}
	return randomID;
}


type HotStringInserter=(hs: HotString | Category)=>void;

function makeDraggable(hs: HotString){
	const element=hs.div;
	element.classList.add("drag");

	function reposition(e: MouseEvent): void{
		const posY=e.clientY;
		let nearest=Infinity;
		let inserter: HotStringInserter=null;

		Category.master.forEach(hs,(y,func/*,currHs,el,pos*/)=>{
			y=Math.abs(posY-y);
			if(y<nearest){
				nearest=y;
				inserter=func;
			}
		});

		//nearest element is itself. its easier to check for null than to check inserter variable
		if(inserter==null) return;

		const prevParent=hs.parent;
		const prevChilds=prevParent.childs.slice();
		inserter(hs);
		if(hs.parent!=prevParent){
			hs.parent.send();
			prevParent.send();
			return;
		}
		const afterChilds=hs.parent.childs;
		if(prevChilds.length!=afterChilds.length){
			hs.parent.send();
			return;
		}
		for(let i=0; i<afterChilds.length; i++)
			if(prevChilds[i]!=afterChilds[i]){
				hs.parent.send();
				return;
			}
	}

	element.addEventListener("mousedown",function(e){
		if(e.target!=element) return;
		e.preventDefault();
		e.stopPropagation();
		reposition(e);
		//element.focus();
		(<HTMLElement>document.activeElement)?.blur?.();
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

interface HotStringBase{
	Id?: number,
	Error?: string,
	Enabled?: boolean,
	Collapsed?: boolean,
}

abstract class HotString{
	public static IdMap: HotString[]=[];
	public static Blocked: HotString[]=[];
	public readonly id: number;
	public parent: Category=null;
	public readonly div: HTMLDivElement;
	public readonly addBefore: HotStringInserter;
	protected readonly titleText: HTMLInputElement;
	private readonly _collapsed: HTMLInputElement;
	private readonly _enabled: HTMLInputElement;
	private readonly errorBox: HTMLElement;
	private readonly options: [HTMLInputElement,(json: any)=>(string | boolean)][]=[];

	protected constructor(parent: Category,data: any){
		if(data==null){
			this.id=0;
			this.div=document.querySelector("#hotstrings");
			return;
		}
		this.id=data.Id;
		HotString.IdMap[this.id]=this;
		this.div=document.createElement("div");
		const isCategory="Category" in data;
		this.div.classList.add(isCategory?"category": "hotstringContainer");
		makeDraggable(this);
		{
			const title=document.createElement("div");
			this.div.appendChild(title);
			title.classList.add("title");

			this._collapsed=<HTMLInputElement>document.createElement("div");
			title.appendChild(this._collapsed);
			this._collapsed.classList.add("collapsed");
			setupCheckbox(this._collapsed,this.div,"collapsed");
			this._collapsed.addEventListener("input",()=>{
				//(<HTMLElement>document.activeElement)?.blur?.();
				this.send();
			});
			this._collapsed.checked=data.Collapsed;

			this._enabled=<HTMLInputElement>document.createElement("div");
			title.appendChild(this._enabled);
			this._enabled.classList.add("enabled");
			setupCheckbox(this._enabled,this.div,"enabled");
			this._enabled.addEventListener("input",()=>{
				this.send();
			});
			this._enabled.checked=data.Enabled!=false;
			//if(data.Enabled==false) this.div.classList.add("enabled");

			this.errorBox=document.createElement("abbr");
			title.appendChild(this.errorBox);
			this.errorBox.textContent="[ERROR]";

			this.titleText=<HTMLInputElement>document.createElement(isCategory?"input": "span");
			title.appendChild(this.titleText);
			if(!isCategory){
				const type=document.createElement("span");
				this.titleText.appendChild(type);
				type.classList.add("type");

				const doubleDot=document.createTextNode(": ");
				this.titleText.appendChild(doubleDot);

				const hotstring=document.createElement("span");
				this.titleText.appendChild(hotstring);
				hotstring.classList.add("hotstring");

				const arrow=document.createTextNode(" ⇒ ");
				this.titleText.appendChild(arrow);

				const replacement=document.createElement("span");
				this.titleText.appendChild(replacement);
				replacement.classList.add("replacement");
			}


			const destroyer=document.createElement("div");
			title.appendChild(destroyer);
			destroyer.classList.add("delete");
			destroyer.classList.add("box");
			destroyer.addEventListener("click",()=>{
				if(!destroyer.classList.contains("r_u_sure")){
					destroyer.classList.add("r_u_sure");
					setTimeout(()=>destroyer.classList.remove("r_u_sure"),1000);
					return;
				}
				const p=this.parent;
				this.destroy();
				p.send();
				this.send();
			});
			const trash=document.createElement("div");
			trash.classList.add("trash");
			destroyer.appendChild(trash);
		}

		parent?.addChild(this);

		this.addBefore=hs=>{
			hs.destroy();
			const i=this.parent.childs.indexOf(this);
			if(i== -1) console.error("HotString is not child of parent");
			else this.parent.childs.splice(i,0,hs);
			hs.parent=this.parent;
			this.parent.div.insertBefore(hs.div,this.div);
		}
	}

	public static update(value: any){
		if(typeof value=="number"){
			this.IdMap[value]?.destroy();
			delete this.IdMap[value];
			return;
		}
		if(Array.isArray(value)){
			Category.master.loadJson(value);
			return;
		}
		const id=value.Id;
		const old=this.IdMap[id];
		if(old) old.loadJson(value);
		else this.get(null,value);
	}

	public static updateBlocked(value: number[]){
		for(let hotString of this.Blocked) hotString.div.classList.remove("blocked");
		this.Blocked=[];

		for(let number of value){
			const hotString=this.get(null,number);
			hotString.div.classList.add("blocked");
			this.Blocked.push(hotString);
		}
	}

	static get(parent: Category,child: any): HotString{
		if(typeof child=="number") return this.IdMap[child];
		if("Category" in child) return new Category(parent,child);
		if("Emoji" in child) return new HotStringEmoji(parent,child);
		if("Regex" in child) return new HotStringRegex(parent,child);
		/*const from=<string>child.From;
		const to=<string>child.To;
		if(typeof from!=="string") throw new Error("From is null");
		if(typeof to!=="string") throw new Error("To is null");
		if(from.length!=to.length) return new HotStringReplace(parent,child);
		if(child.keepCase!=false) return new HotStringKeepCase(parent,child);*/
		return new HotStringReplace(parent,child);
	}

	addOption(func: (json: any)=>(string | boolean),name: string,sub?: string){
		const row=document.createElement("tr");
		this.div.appendChild(row);

		const nameElement=document.createElement("td");
		row.appendChild(nameElement);
		nameElement.textContent=name;
		if(sub){
			const subElement=document.createElement("sub");
			subElement.textContent=sub;
			nameElement.appendChild(subElement);
		}

		const inputContainer=document.createElement("td");
		row.appendChild(inputContainer);
		let input: HTMLInputElement;
		//checks type with empty object. If this control should be a checkbox a boolean is returned otherwise any gibberish means text
		if(typeof (func({}))==="boolean"){
			input=<HTMLInputElement>document.createElement("div");
			setupCheckbox(input);
		}else{
			input=document.createElement("input");
			input.addEventListener("blur",()=>{
				send("hotStringBlock="+0);
			});
			input.addEventListener("focus",()=>{
				send("hotStringBlock="+this.id);
			});
		}
		input.addEventListener("input",()=>{
			this.updateControls();
			this.send()
		});

		inputContainer.appendChild(input);

		this.options.push([input,func]);
		return input;
	}

	forEach(hs: HotString,func: (y: number,func: HotStringInserter,currHs: HotString,el: Element,type: string)=>void): void{
		if(this==hs) func(this.div.getBoundingClientRect().top,null,this,this.div,"self hs");
		else func(this.div.getBoundingClientRect().top,this.addBefore,this,this.div,"before");
	}

	destroy(removeHtml: boolean=true): void{
		if(this.parent==null) return;
		const i=this.parent.childs.indexOf(this);
		if(i== -1) console.error("HotString is not child of parent");
		else this.parent.childs.splice(i,1);

		if(removeHtml) this.div.parentElement.removeChild(this.div);
		this.parent=null;
	}

	abstract toJson(): object;

	toFullJson(): object{
		const o: any=this.toJson();
		o.Enabled=(this.div.classList.contains("enabled"))&&undefined;
		o.Collapsed=this.div.classList.contains("collapsed")||undefined;
		o.Error=this.errorBox.title||undefined;
		o.Id=this.id;
		return o;
	}

	loadJson(json: HotStringBase){
		for(let [input,func] of this.options){
			const value=func(json);
			if(typeof value=="boolean") input.checked=value;
			else input.value=value||"";
		}

		if(this.errorBox){
			if(json.Error) this.errorBox.title=json.Error;
			else this.errorBox.removeAttribute("title");

			this._enabled.checked=json.Enabled!=false;
			//else this.div.classList.remove("disabled");
			if(json.Collapsed) this.div.classList.add("collapsed");
			else this.div.classList.remove("collapsed");
		}

		this.updateControls();
	}

	send(): void{
		this.updateControls();
		let value: any;
		if(this.parent==null){
			value=this.id;
			delete HotString.IdMap[this.id];
		}else value=this.toFullJson();

		send("hotString="+JSON.stringify(value));
	}

	abstract getFromTo(): [string,string,string];

	protected updateControls(): void{
		const [type,from,to]=this.getFromTo();
		this.titleText.querySelector(".type").textContent=type;
		this.titleText.querySelector(".hotstring").textContent=from;
		this.titleText.querySelector(".replacement").textContent=to;
	}
}

interface CategoryData extends HotStringBase{
	Category: string;
	Children?: any[];
}

class Category extends HotString{
	public readonly div: HTMLDivElement;
	public readonly addChild: HotStringInserter;
	public readonly addBefore: HotStringInserter;
	public readonly childs: HotString[]=[];
	private readonly createNew: HTMLDivElement;

	constructor(parent: Category,data: CategoryData){
		super(parent,data);
		if(data==null){
			this.addBefore=hs=>{
				console.log("can't add HotString before Master: ",hs)
			};
			const thiz=this;
			Object.defineProperty(this.div,"value",{
				get(): any{
					thiz.toJsonArray();
				},
				set(v: any): void{
					thiz.loadJson(v);
				}
			});
		}else{
			this.titleText.value=data.Category;
			this.titleText.addEventListener("input",()=>{
				this.send();
			});

			this.addBefore=hs=>{
				hs.destroy(false);
				const i=this.parent.childs.indexOf(this);
				if(i== -1) console.error("Category is not child of parent");
				else this.parent.childs.splice(i,0,hs);
				hs.parent=this.parent;
				this.parent.div.insertBefore(hs.div,this.div);
			}
		}

		this.createNew=document.createElement("div");
		this.div.appendChild(this.createNew);
		this.createNew.classList.add("addNew");

		const addOption=(name: string,create: ()=>(HotString | Category))=>{
			const div=document.createElement("div");
			this.createNew.appendChild(div);
			div.textContent=name;
			div.addEventListener("click",()=>{
				create().send();
				this.send();
			});
		};

		addOption("Category",()=>new Category(this,{
			Id:getRandomId(),
			Category:"Category"
		}));
		addOption("Emoji",()=>new HotStringEmoji(this,{
			Id:getRandomId(),
			From:"xdd",
			Emoji:"😂"
		}));
		addOption("Regex",()=>new HotStringRegex(this,{
			Id:getRandomId(),
			Regex:"(?<=^| )itn$",
			Replacement:"int",
			IgnoreCase:false
		}));
		addOption("Replace",()=>new HotStringReplace(this,{
			Id:getRandomId(),
			From:"cosnt",
			To:"const",
			IgnoreCase:false
		}));


		this.addChild=(hs: HotString)=>{
			hs.destroy(false);
			this.childs.push(hs);
			hs.parent=this;
			this.div.insertBefore(hs.div,this.createNew);
		};
		const childs=data?.Children;
		if(childs!=null)
			for(let i=0; i<childs.length; i++)
				this.addChild(HotString.get(this,childs[i]));
	}

	private static _master: Category;

	public static get master(){
		if(this._master==null) this._master=new Category(null,null);
		return this._master;
	}

	destroy(): void{
		if(this.parent==null) return;
		const i=this.parent.childs.indexOf(this);
		if(i== -1) console.error("Category is not child of parent");
		else this.parent.childs.splice(i,1);

		//this.div.remove();
		this.div.parentElement.removeChild(this.div);
		this.parent=null;
	}

	forEach(hs: HotString,func: (y: number,func: HotStringInserter,currHs: HotString,el: Element,type: string)=>void): void{
		if(this==hs){
			if(this.parent!=null)
				func(this.div.getBoundingClientRect().top,null,this,this.div,"self category");
			return;
		}
		if(this.parent!=null)
			func(this.div.getBoundingClientRect().top,this.addBefore,this,this.div,"before");

		if(this.div.classList.contains("collapsed")) return;

		for(let child of this.childs) child.forEach(hs,func);

		func(this.createNew.getBoundingClientRect().top,this.addChild,this,this.div,"child");
	}

	toJson(): CategoryData | any[]{
		return this==Category.master?this.toJsonArray(): {
			Category:this.titleText.value,
			Children:this.childs.length?this.toJsonArray(): undefined
		};
	}

	toJsonArray(): any[]{
		const arr=[];
		for(let child of this.childs) arr.push(child.id);
		return arr;
	}

	loadJson(json: any){
		super.loadJson(json);
		if(!Array.isArray(json)){
			this.titleText.value=json.Category;
			json=json.Children;
		}
		const arr: any[]=json?json: [];
		for(let i=this.childs.length-1; i>=0; i--)
			this.childs[i].destroy(arr.indexOf(this.childs[i].id)== -1);//only remove html if needed
		for(let child of arr)
			this.addChild(HotString.get(this,child));
	}

	send(): void{
		let value: any;
		if(this==Category.master) value=this.toJsonArray();
		else if(this.parent==null){
			delete HotString.IdMap[this.id];
			value=this.id;
			//freeing children
			for(let i=this.childs.length-1; i>=0; i--){
				const child=this.childs[i];
				child.destroy();
				child.send();
			}
		}else value=this.toFullJson();

		send("hotString="+JSON.stringify(value));
	}

	getFromTo(): [string,string,string]{
		return [null,null,null];
	}

	protected updateControls(): void{
	}
}


interface HotStringEmojiData extends HotStringBase{
	From: string,
	Emoji: string,
	Regex?: string,
	IgnoreCase?: boolean
}

class HotStringEmoji extends HotString{
	private readonly _from: HTMLInputElement;
	private readonly _regex: HTMLInputElement;
	private readonly _ignoreCase: HTMLInputElement;
	private readonly _emoji: HTMLInputElement;

	constructor(parent: Category,data: HotStringEmojiData){
		super(parent,data);
		this._from=this.addOption(j=>j.From,"From");
		this._regex=this.addOption(j=>j.Regex,"Regex","(optional)");
		this._ignoreCase=this.addOption(j=>j.IgnoreCase==true,"IgnoreCase");
		this._emoji=this.addOption(j=>j.Emoji,"Emoji");
		this._emoji.classList.add("emoji");

		this.loadJson(data);
	}

	toJson(): HotStringEmojiData{
		return {
			From:this._from.value,
			Emoji:this._emoji.value,
			Regex:this._regex.value||undefined,//use value, but if empty string then dont send anything
			IgnoreCase:this._ignoreCase.checked||undefined
		}
	}

	getFromTo(): [string,string,string]{
		return ["Emoji",this._from.value,this._emoji.value];
	}
}

interface HotStringRegexData extends HotStringBase{
	Regex: string,
	Replacement: string,
	IgnoreCase: boolean
}

class HotStringRegex extends HotString{
	private readonly _replacement: HTMLInputElement;
	private readonly _regex: HTMLInputElement;
	private readonly _ignoreCase: HTMLInputElement;

	constructor(parent: Category,data: HotStringRegexData){
		super(parent,data);
		this._regex=this.addOption(j=>j.Regex,"Regex");
		this._ignoreCase=this.addOption(j=>j.IgnoreCase==true,"IgnoreCase");
		this._replacement=this.addOption(j=>j.Replacement,"Replacement");

		this.loadJson(data);
	}

	toJson(): HotStringRegexData{
		return {
			Regex:this._regex.value,
			IgnoreCase:this._ignoreCase.checked,
			Replacement:this._replacement.value
		}
	}

	getFromTo(): [string,string,string]{
		return ["Regex",this._regex.value,this._replacement.value];
	}
}

interface HotStringReplaceData extends HotStringBase{
	From: string,
	To: string,
	IgnoreCase?: boolean,
	KeepCase?: boolean
}

class HotStringReplace extends HotString{
	private readonly _from: HTMLInputElement;
	private readonly _ignoreCase: HTMLInputElement;
	private readonly _to: HTMLInputElement;
	private readonly _keepCase: HTMLInputElement;

	constructor(parent: Category,data: HotStringReplaceData){
		super(parent,data);
		this._from=this.addOption(j=>j.From,"From");
		this._ignoreCase=this.addOption(j=>j.IgnoreCase==true,"IgnoreCase");
		this._to=this.addOption(j=>j.To,"To");
		this._keepCase=this.addOption(j=>j.KeepCase!=false,"KeepCase");

		this.loadJson(data);
	}

	updateControls(): void{
		super.updateControls();
		if(this._from.value.length==this._to.value.length){
			this._keepCase.removeAttribute("disabled");
		}else{
			this._keepCase.setAttribute("disabled","true");
			this._keepCase.checked=false;
		}
		if(this._keepCase.checked){
			this._ignoreCase.setAttribute("disabled","true");
			this._ignoreCase.checked=true;
		}else this._ignoreCase.removeAttribute("disabled");
	}

	toJson(): HotStringReplaceData{
		return {
			From:this._from.value,
			IgnoreCase:this._ignoreCase.checked||undefined,
			To:this._to.value,
			KeepCase:this._from.value.length==this._to.value.length&& !this._keepCase.checked?false: undefined
		}
	}

	getFromTo(): [string,string,string]{
		return ["Replace",this._from.value,this._to.value];
	}
}

//endregion

//region Key Combos
function initKeyCombos(){
	const keycombos=document.getElementsByClassName("keycombo");
	for(let i=0; i<keycombos.length; i++){
		const keycombo=keycombos[i];
		setKeyCombo(keycombo,keycombo.textContent);
	}
}

function setKeyCombo(element: Element,keycombo: string): void{
	const fragment=document.createDocumentFragment();
	let first=true;
	for(let combo of keycombo.split(/ *\| */)){
		if(first) first=false;
		else fragment.appendChild(document.createTextNode(" | "));

		for(let key of combo.split(/ *\+ */)){

			const keyElement=document.createElement("span");
			keyElement.classList.add("key");
			keyElement.textContent=key;
			fragment.appendChild(keyElement);
		}
	}
	while(element.firstChild) element.removeChild(element.lastChild);
	element.appendChild(fragment);
}

//endregion

//region On Change Listener
function updateElement(target: HTMLInputElement | HTMLTextAreaElement): boolean{
	if("rows" in target){
		target.rows=1;
		const parent=target.parentElement;
		const preStyle=parent.getAttribute("style");
		parent.style.height=parent.clientHeight+"px";
		target.style.height="auto";
		target.style.height=target.scrollHeight+"px";
		if(preStyle) parent.setAttribute("style",preStyle);
		else parent.removeAttribute("style");
	}

	function applyAnyNumber(){
		if(target.value.length==0){
			target.value="0";
			target.setSelectionRange(1,1);
		}else while(target.value.length!=1&&(target.value[0]=="0"||target.value[0]=="-")){
			const oldSelectionStart=target.selectionStart-1;
			const oldSelectionEnd=target.selectionEnd-1;
			target.value=target.value.substring(1);
			target.setSelectionRange(oldSelectionStart,oldSelectionEnd);
		}
		//FIXME broken caret backtracing, if moving caret in between inputs
	}

	if(target.classList.contains("long")){
		applyAnyNumber();
		if((+target.value)+""===target.value&&/^\d+$/.test(target.value))//if string representation of converted is same
			(<any>target).old=[target.value,target.selectionStart,target.selectionEnd];
		else{
			const [value="",start,end]=(<any>target).old;
			target.value=value;
			target.setSelectionRange(start,end);
		}
	}else if(target.classList.contains("int")){
		applyAnyNumber();
		if((+target.value|0)+""===target.value)//if string representation of converted is same ("|0" is used to clamp to int)
			(<any>target).old=[target.value,target.selectionStart,target.selectionEnd];
		else{
			const [value="",start,end]=(<any>target).old;
			target.value=value;
			target.setSelectionRange(start,end);
		}
	}else if(target.classList.contains("byte")){
		applyAnyNumber();

		if(/^-?([1-9]?\d|1\d\d|2[0-4]\d|25[0-5])$/.test(target.value))
			(<any>target).old=[target.value,target.selectionStart,target.selectionEnd];
		else{
			const [value="",start,end]=(<any>target).old;
			target.value=value;
			target.setSelectionRange(start,end);
		}
	}
	target.classList.remove("error");
	target.removeAttribute("title");
	return false;
}

function initChangeListener(){
	const named=document.querySelectorAll("input[name],textarea[name],.check.box[name]");
	for(let i=0; i<named.length; i++){
		const element=named[i];
		element.addEventListener("input",onInput);
		updateElement(<HTMLInputElement | HTMLTextAreaElement>element);
	}
	const now=document.querySelectorAll(".now");
	for(let i=0; i<now.length; i++){
		const element=now[i];
		element.addEventListener("input",onInput);
		updateElement(<HTMLInputElement | HTMLTextAreaElement>element);
	}
}

function onInput(evt: Event){
	const target=<HTMLInputElement>evt.target;

	const b=updateElement(target);
	if(b){
		evt.preventDefault();
		evt.stopPropagation();
	}
	if(target.name.indexOf("=")!= -1){
		send(target.name);
		return;
	}
	const value: string | boolean=target.type=="checkbox"?target.checked: target.value;

	const key=target.classList.contains("now")?target.id: target.name;
	send(key+"="+JSON.stringify(value));
}

function setupCheckbox(checkbox: HTMLInputElement,classElement: HTMLElement=null,clazz: string="checked"){
	checkbox.tabIndex=0;
	if(classElement==null){
		if(!checkbox.hasAttribute("name")||checkbox.getAttribute("name").indexOf("=")== -1)
			checkbox.classList.add("check");
		classElement=checkbox;
	}
	checkbox.classList.add("box");
	checkbox.type="checkbox";
	Object.defineProperty(checkbox,"name",{
		get(): any{
			return checkbox.getAttribute("name");
		},
		set(v: any): void{
			if(v!=undefined) checkbox.setAttribute("name",v);
			else checkbox.removeAttribute("name");
		}
	});
	Object.defineProperty(checkbox,"checked",{
		get(): any{
			return classElement.classList.contains(clazz);
		},
		set(v: any): void{
			if(v) classElement.classList.add(clazz);
			else classElement.classList.remove(clazz);
		}
	});
	Object.defineProperty(checkbox,"disabled",{
		get(): any{
			return checkbox.hasAttribute("disabled");
		},
		set(v: any): void{
			if(v) checkbox.setAttribute("disabled","true");
			else checkbox.removeAttribute("disabled");
		}
	});

	function toggle(){
		classElement.classList.toggle(clazz);
		let event: Event;
		if(typeof Event==="function") event=new Event("input");
		else{
			event=document.createEvent("Event");
			event.initEvent("input",false,false);
		}
		checkbox.dispatchEvent(event);

		if((checkbox.name||"").indexOf("=")!= -1)
			receive(checkbox.name);
	}

	checkbox.addEventListener("click",()=>{
		if(checkbox.hasAttribute("disabled")) return;
		checkbox.focus();
		toggle();
	});
	checkbox.addEventListener("keypress",(e)=>{
		if([" ","Space","Spacebar"].indexOf(e.key)== -1) return;
		e.preventDefault();
		toggle();
	});
}

//endregion
