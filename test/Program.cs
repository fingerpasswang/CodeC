using System;
using System.Linq;
using CodeC;

namespace CodeC.Test
{
    class Program
    {
        class Meta1
        {
            public string Type;
            public string Name;
            public string Value;
            public bool IsConfigable;
            public int Id;
        }

        static void Test()
        {
            var classHead = Generator.GenBasic((Meta1 m) => String.Format("public class {0}", m.Type));
            //new Func<Meta1, string>(m=> String.Format("public class {0}", m.Type)).Basic();
        }
        static void Main(string[] args)
        {
            var basicCoder = Generator.GenBasic((Meta1 m) => string.Format(@"{0} {1} = {2}", m.Type, m.Name, m.Value));

            var meta = new Meta1()
            {
                Type = "float",
                Name = "EntityTranslateSpeed",
                Value = "0.008f",
                IsConfigable = true,
                Id = 1,
            };
            var meta2 = new Meta1()
            {
                Type = "int",
                Name = "EntityRotateSpeed",
                Value = "3f",
                IsConfigable = true,
                Id = 3,
            };
            var meta3 = new Meta1()
            {
                Type = "MyType",
                Name = "ActivateDragMapTime",
                Value = "0.2f",
                IsConfigable = false,
                Id = 5,
            };

            var metas = new[] { meta, meta2, meta3 };

            {
                var floatCoder = "FloatOutput".Unit<Meta1>().Satisfy(meta1 => meta1.Type.Equals("float"));
                var intCoder = "IntOutput".Unit<Meta1>().Satisfy(meta1 => meta1.Type.Equals("int"));
                var typeCoder = "MyOutput".Unit<Meta1>();
                var select = Generator.GenSelect(floatCoder, intCoder, typeCoder);

                //Console.WriteLine(floatCoder.Code(meta2));

                Console.WriteLine(select.Code(meta));
                Console.WriteLine(select.Code(meta2));
                Console.WriteLine(select.Code(meta3));
            }

            var regionCoder = basicCoder.WithStatic().WithPublic().Statement().Many("\n").Region("Static Getter");

            Console.WriteLine(regionCoder.Code(metas));
            {
                
                Console.WriteLine("test");
            }
            {
                // Persistence
                ICoder<Meta1[]> loadFunc = null;
                ICoder<Meta1[]> saveFunc = null;
                {
                    // function test
                    var sigCoder = Generator.GenUnit("void Load()").WithStatic().WithPublic();
                    
                    loadFunc = Generator
                        .GenBasic((Meta1 m) => string.Format("{0} = PlayerPrefs.Get{1}(\"{0}\", {2})", m.Name, m.Type, m.Value))
                        .Statement()
                        .Many("\n", m => m.IsConfigable)
                        .Function(sigCoder, m => new object());
                }

                {
                    // function test
                    var sigCoder = Generator.GenUnit("void Save()").WithStatic().WithPublic();
                    
                    saveFunc = Generator
                        .GenBasic((Meta1 m) => string.Format("{0} = PlayerPrefs.Set{1}(\"{0}\", {0})", m.Name, m.Type))
                        .Statement()
                        .Many("\n", m => m.IsConfigable)
                        .Function(sigCoder, (m) => new object());
                }

                // combine save and load
                // with region wrapped

                var coder = loadFunc.Combine(saveFunc, ms => ms).Region("Persistence");

                Console.WriteLine(coder.Code(metas));
            }

            {
                // switch 
                var coder = Generator.GenSwitch(
                    Generator.GenUnit("index")
                    , Generator.GenBasic((Meta1 m) => m.Id.ToString())
                    , Generator.GenBasic((Meta1 m) => string.Format("val = GameConfig.{0}", m.Name)).Statement()
                    , Generator.GenUnit("val = 0f").Statement());

                Console.WriteLine(coder.Code(metas));

                var coder2 = Generator.GenSwitch(
                    Generator.GenUnit("index")
                    , Generator.GenBasic((Meta1 m) => m.Id.ToString())
                    , Generator.GenBasic((Meta1 m) => string.Format("setter = val => GameConfig.{0} = val", m.Name)).Statement()
                    , Generator.GenUnit("setter = val => 0f").Statement());

                Console.WriteLine(coder2.Code(metas));
            }
        }
    }
}
