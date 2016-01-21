Basic concepts

ICoder<T>  :: T -> string
	a coder that reads a meta of T and outpus a string

meta class definition

class MethodMeta
{
	public string Name;
	public uint Id;
}
class ServiceMeta
{
	public string Name;
	public uint Id;
	public List<MethodMeta> Methods = new List<MethodMeta>();
}

var meta = new ServiceMeta()
{
	Name = "ClientLogic",
	Id = 1,
	Methods = new List<MethodMeta>()
	{
		new MethodMeta(){Name="AskLogin",Id=1},
		new MethodMeta(){Name="AskAddMoney",Id=2},
	},
};
	
ZeroCoder<T> :: ICoder<T>
	a coder that outputs nothing whatever it reads
	
	Usage
	var coder = new ZeroCoder<ServiceMeta>();
	For simplicity
	var coder = ZeroCoder<ServiceMeta>.Instance;
	
	coder.Code(meta); 
	// ""
	
UnitCoder<T> :: ICoder<T>
	a coder that outputs an identical string whatever it reads
	
	Usage 
	var coder = new UnitCoder<ServiceMeta>("hello, world");
	
	For simplicity
	var coder = "hello, world".Unit<ServiceMeta>();
	
	coder.Code(meta); 
	// "hello, world"
	
BasicCoder<T> :: ICoder<T>
	a coder that reads metas and fills into a template of which it is completely comprised.
	
	Usage
	var coder = new BasicCoder<ServiceMeta>(meta => string.Format("I{0}Service", meta.Name));
	
	For simplicity
	var coder = "I{0}Service".Basic<ServiceMeta>(meta => meta.Name);
	
	coder.Code(meta); 
	// "IClientLogicService"
	
RepeatedCoder<T> :: ICoder<IEnumerable<T>>
	take a ICoder<T> then convert to a ICoder<IEnumerable<T>>, which means a repeated ICoder<T>

	Usage 
	var method = new BasicCoder<MethodMeta>(meta => string.Format("method{0}_{1}", meta.Name, meta.Id));
	
	var methods = new RepeatedCoder<MethodMeta>(method, "\n");
	
	For simplicity
	var methods = 
		"method{0}_{1}"
			.Basic<MethodMeta>(meta => meta.Name,  meta => meta.Id)
		.Many("\n");
	
	methods.Code(meta.methods); 
	// methodAskLogin_1
	// methodAskAddMoney_2
	
SequenceCoder<T> :: ICoder<T> 
	combine several coders sequentially
	
	Usage
	var method1 = new BasicCoder<MethodMeta>(meta => string.Format("method{0}_{1}", meta.Name, meta.Id));
	var method2 = new BasicCoder<MethodMeta>(meta => string.Format(" Name={0} Id={1}", meta.Name, meta.Id));
	var method = new SequenceCoder<MethodMeta>(method1, method2);
	
	For simplicity
	var method = Generator.GenSequence(
		"method{0}_{1}".Basic<MethodMeta>(meta => meta.Name,  meta => meta.Id)
	   ," Name={0} Id={1}".Basic<MethodMeta>(meta => meta.Name,  meta => meta.Id));
	
	method.Code(meta.methods);
	// methodAskLogin_1 Name=AskLogin Id=1
	// methodAskAddMoney_2 Name=AskAddMoney Id=2
	
SatisfyCoder<T> :: ICoder<T>
	decorate a coder with a condition which must be true for the decorated coder to output as before, or it would output nothing
	
	Usage
	var method1 = new BasicCoder<MethodMeta>(meta => string.Format("method{0}_{1}", meta.Name, meta.Id));
	var smethod = new SatisfyCoder<MethodMeta>(method1, meta => meta.Id
	 == 1); // 只会输出Id为1的method
	
	For simplicity
	var smethod = 
	"method{0}_{1}"
		.Basic<MethodMeta>(meta => meta.Name,  meta => meta.Id)
	.Satisfy(meta => meta.Id == 1);
	
	method.Code(meta.methods[0]); // "methodAskLogin_1"
	method.Code(meta.methods[1]); // ""
	
SelectCoder<T> :: ICoder<T>
	combine several coders to one coder, which stands for selectivity semantically.When drive a SelectCoder<T>, it would test coders it consists of in order and stop for the first one whose condition is satisfied.
	
	Usage
	var smethod1 = 
	"method{0}_{1}"
		.Basic<MethodMeta>(meta => meta.Name,  meta => meta.Id)
	.Satisfy(meta => meta.Id == 1);
	var smethod2 = 
	"Name={0} Id={1}"
		.Basic<MethodMeta>(meta => meta.Name,  meta => meta.Id)
	.Satisfy(meta => meta.Id == 2);
	
	var method = new SelectCoder<MethodMeta>(smethod1, smethod2);
	
	For simplicity
	var method = Generator.GenSelect(
		"method{0}_{1}"
			.Basic<MethodMeta>(meta => meta.Name,  meta => meta.Id)
		.Satisfy(meta => meta.Id == 1)
	   ,"Name={0} Id={1}"
			.Basic<MethodMeta>(meta => meta.Name,  meta => meta.Id)
		.Satisfy(meta => meta.Id == 2));
	
	method.Code(meta.methods);
	// methodAskLogin_1
	// Name=AskAddMoney Id=2
	
	
	
	
	
	
	
	