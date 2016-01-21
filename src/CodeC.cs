using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace CodeC
{
    public class Generator
    {
        public static ICoder<T> GenZero<T>()
        {
            return ZeroCoder<T>.Instance;
        }

        public static ICoder<object> GenUnit(string output)
        {
            return new UnitCoder<object>(output);
        }

        public static ICoder<T> GenBasic<T>(Func<T, string> func) where T : class
        {
            return new BasicCoder<T>(func);
        }

        #region common combinators
        public static ICoder<T> GenRegion<T>(ICoder<T> coder, string name) where T : class
        {
            var coderBegin = new UnitCoder<T>(string.Format("\n#region {0}", name));
            var coderEnd = new UnitCoder<T>("#endregion\n");

            return new SequenceCoder<T>(coderBegin.Code
                                      , GenUnit("\n").Code
                                      , coder.Code
                                      , GenUnit("\n").Code
                                      , coderEnd.Code);
        }
        #endregion

        #region extend combinators
        public static ICoder<IEnumerable<T>> GenRepeated<T>(ICoder<T> coder, string seperator) where T : class
        {
            return new RepeatedCoder<T>(coder, seperator, (m)=>true);
        }
        public static ICoder<IEnumerable<T>> GenRepeated<T>(ICoder<T> coder, string seperator, Func<T, bool> pred) where T : class
        {
            return new RepeatedCoder<T>(coder, seperator, pred);
        }
        #endregion

        public static ICoder<T> GenSelect<T>(params ICoder<T>[] coders)
        {
            return new SelectCoder<T>(coders);
        }

        public static ICoder<T> GenCombine<T, T1>(ICoder<T> tCoder, ICoder<T1> t1Coder, Func<T, T1> selector)
        {
            return new SequenceCoder<T>(tCoder.Code, meta => t1Coder.Code(selector(meta)));
        }

        public static ICoder<T> GenCombineReverse<T, T1>(ICoder<T1> tCoder, ICoder<T> t1Coder, Func<T, T1> selector)
            where T : class
            where T1 : class
        {
            return new SequenceCoder<T>(meta => tCoder.Code(selector(meta)), t1Coder.Code);
        }

        public static ICoder<T> GenFunction<T, T1>(ICoder<T1> signatureCoder, ICoder<T> bodyCoder, Func<T, T1> selector) 
            where T  : class
            where T1 : class
        {
            var body = bodyCoder.WithPrefix("\n").WithPostfix("\n").Brace().WithPostfix("\n");

            return GenCombineReverse(signatureCoder.WithPostfix("\n"), body, selector);
        }

        public static ICoder<IEnumerable<T>> GenSwitch<T>(ICoder<object> switchCoder, ICoder<T> labelCoder, ICoder<T> statementCoder, ICoder<object> defaultCoder)
            where T : class
        {
            var sing = labelCoder.WithPrefix("\tcase ").WithPostfix(":").Combine(statementCoder, Id).WithPostfix("break;");
            var body = sing.Many("\n").WithPostfix("\n\tdefault:").Combine(defaultCoder, m => new object()).Between("\n").Brace();

            return body.CombineReverse(switchCoder.Bracket().WithPrefix("switch").WithPostfix("\n"), m => new object());
        }

        public static ICoder<T> GenSequence<T>(params ICoder<T>[] coders)
        {
            return new SequenceCoder<T>(coders.Select<ICoder<T>, Func<T, string>>(coder => (T meta) => coder.Code(meta)).ToArray());
        }

        public static T Id<T>(T t)
        {
            return t;
        }
    }

    public static class CoderExtensions
    {
        #region prefix and suffix
        public static ICoder<T> SkipLine<T>(this ICoder<T> coder, int num = 1) where T : class
        {
            var postFix = new char[num];
            for (int i = 0; i < num; i++)
            {
                postFix[i] = '\n';
            }

            return coder.WithPostfix(new string(postFix));
        }

        public static ICoder<T> WithPostfix<T>(this ICoder<T> coder, string postfix) where T : class
        {
            var coderPostfix = new UnitCoder<T>(postfix);

            return new SequenceCoder<T>(coder.Code, coderPostfix.Code);
        }
        public static ICoder<T> WithPrefix<T>(this ICoder<T> coder, string prefix) where T : class
        {
            var coderPrefix = new UnitCoder<T>(prefix);

            return new SequenceCoder<T>(coderPrefix.Code, coder.Code);
        }
        public static ICoder<T> Statement<T>(this ICoder<T> coder) where T : class
        {
            return coder.WithPostfix(";");
        }
        public static ICoder<T> WithPublic<T>(this ICoder<T> coder) where T : class
        {
            return coder.WithPrefix("public ");
        }
        public static ICoder<T> WithPrivate<T>(this ICoder<T> coder) where T : class
        {
            return coder.WithPrefix("private ");
        }
        public static ICoder<T> WithStatic<T>(this ICoder<T> coder) where T : class
        {
            return coder.WithPrefix("static ");
        }

        public static ICoder<T> Between<T>(this ICoder<T> coder, string str) where T : class
        {
            return coder.WithPostfix(str).WithPrefix(str);
        }
        #endregion

        #region repeat

        public static ICoder<IEnumerable<T>> Many<T>(this ICoder<T> coder, string seperator="") where T : class
        {
            return Generator.GenRepeated(coder, seperator);
        }
        public static ICoder<IEnumerable<T>> Many<T>(this ICoder<T> coder, string seperator, Func<T, bool> pred) where T : class
        {
            return Generator.GenRepeated(coder, seperator, pred);
        }
        #endregion

        #region wrap
        // #region #endregion
        public static ICoder<T> Region<T>(this ICoder<T> coder, string name) where T : class
        {
            return Generator.GenRegion(coder, name);
        }

        // { }
        public static ICoder<T> Brace<T>(this ICoder<T> coder) where T : class
        {
            return coder.WithPostfix("}").WithPrefix("{");
        }

        // ()
        public static ICoder<T> Bracket<T>(this ICoder<T> coder) where T : class
        {
            return coder.WithPostfix(")").WithPrefix("(");
        }
        #endregion

        public static Func<T, T> Id<T>(this T t)
        {
            return tt => t;
        }

        public static ICoder<T> Basic<T>(this Func<T, string> func)
        {
            return new BasicCoder<T>(func);
        }

        public static ICoder<object> Unit(this string str)
        {
            return new UnitCoder<object>(str);
        }

        public static ICoder<T> Basic<T>(this string formatter, params Func<T,object>[] args)
        {
            return new BasicCoder<T>(meta => string.Format(formatter, args.Select(m=>m(meta)).ToArray()));
        }

        public static ICoder<T> Unit<T>(this string str)
        {
            return new UnitCoder<T>(str);
        }

        public static ICoder<T> Satisfy<T>(this ICoder<T> coder, Func<T, bool> pred)
        {
            return new SatisfyCoder<T>(coder, pred);
        }

        public static ICoder<T> Append<T>(this ICoder<T> coder, ICoder<T> coder2)
            where T : class
        {
            return Combine(coder, coder2, Generator.Id);
        }

        public static ICoder<T> Combine<T, T1>(this ICoder<T> coder, ICoder<T1> coder2, Func<T, T1> selector)
        {
            return Generator.GenCombine(coder, coder2, selector);
        }
        public static ICoder<T> CombineReverse<T, T1>(this ICoder<T> coder, ICoder<T1> coder2, Func<T, T1> selector)
            where T : class
            where T1 : class
        {
            return Generator.GenCombineReverse(coder2, coder, selector);
        }

        public static ICoder<T> Lift<T, T1>(this ICoder<T1> coder, Func<T, T1> selector)
        {
            return Generator.GenCombine(new ZeroCoder<T>(), coder, selector);
        }

        #region custom
        public static ICoder<T> Function<T, T1>(this ICoder<T> coder, ICoder<T1> signatureCoder, Func<T, T1> selector)
            where T : class
            where T1 : class
        {
            return Generator.GenFunction(signatureCoder, coder, selector);
        }
        #endregion
    }

    // this class helper just for proj-specific ,which can be taken at hand
    // you can simply delete this
    public static class CoderHelper
    {
        //static ICoder<Type> TypeCoder()
        //{
        //    var voidCoder = "void".Unit<Type>().Satisfy(t => t.Name.Equals("Void"));
        //    var normalCoder = new Func<Type, string>(t => t.Name).Basic().Satisfy(t => !t.IsGenericType);

        //    var genericCoder = new Func<Type, string>(t =>
        //        string.Format("{0}<{1}>", t.Name.Substring(0, t.Name.IndexOf('`')), "aaa"));

        //}

        public static string GetTypeStr(Type type)
        {
            string typeStr = "";

            if (type.IsGenericType)
            {
                var types = type.GetGenericArguments();
                var genericParaStr = "";

                for (int i = 0; i < types.Length; i++)
                {
                    var genericPara = types[i];
                    var name = GetTypeStr(genericPara);

                    if (i != 0)
                    {
                        genericParaStr += ", ";
                    }

                    genericParaStr += name;
                }

                typeStr = string.Format("{0}<{1}>", type.Name.Substring(0, type.Name.IndexOf('`')), genericParaStr);
            }
            else
            {
                typeStr = type.Name;
            }

            if (typeStr == "Void")
            {
                typeStr = "void";
            }

            return typeStr;
        }

        // Task/Task<bool>
        public static ICoder<Type> TaskTypeCoder()
        {
            var taskTypeName = Generator.GenSelect(
                                   "Task"
                                   .Unit<Type>()
                                   .Satisfy(meta => meta.Name.ToLower().Equals("void")),
                                   "Task<{0}>"
                                   .Basic<Type>(CoderHelper.GetTypeStr));

            return taskTypeName;
        }

        // bw.Write({0});
        // bw.Write((int){0});
        // bw.Write({0}.Length);\nbw.Write({0});
        // {0}.Write(bw);
        public static ICoder<KeyValuePair<Type, string>> BaseWriteCoder(int countPostfix = 0)
        {
            var baseWrite = Generator.GenSelect(
                    "bw.Write(({1}){0});".Basic<KeyValuePair<Type, string>>(meta => meta.Value, meta=>GetTypeStr(meta.Key))
                        .Satisfy(meta => meta.Key.IsPrimitive || meta.Key == typeof(string))
                    ,
                    "bw.Write((int){0});".Basic<KeyValuePair<Type, string>>(meta => meta.Value)
                        .Satisfy(meta => meta.Key.IsEnum)
                    ,
                    "bw.Write(((byte[]){0}).Length);\nbw.Write((byte[]){0});".Basic<KeyValuePair<Type, string>>(meta => meta.Value)
                        .Satisfy(meta => meta.Key == typeof(byte[]))
                    ,
                    "(({1}){0}).Write(bw);".Basic<KeyValuePair<Type, string>>(meta => meta.Value, meta => GetTypeStr(meta.Key)));

            var listWrite =
                "bw.Write((({2}){0}).Count);\nforeach (var item{3} in ({2}){0}){{{1}}}".Basic<KeyValuePair<Type, string>> (
                        meta => meta.Value
                        , meta => BaseWriteCoder(countPostfix+1).Code(new KeyValuePair<Type, string>(meta.Key.GetGenericArguments()[0], "item"+countPostfix.ToString()))
                        , meta=>GetTypeStr(meta.Key)
                        , _ => countPostfix.ToString())
                    .Satisfy(meta => meta.Key.Name == "List`1");

            var arrayWrite =
                "bw.Write((({2}){0}).Length);\nforeach (var item{3} in ({2}){0}){{{1}}}".Basic<KeyValuePair<Type, string>>(
                        meta => meta.Value
                        , meta => BaseWriteCoder(countPostfix + 1).Code(new KeyValuePair<Type, string>(meta.Key.GetElementType(), "item" + countPostfix.ToString()))
                        , meta=>GetTypeStr(meta.Key)
                        , _ => countPostfix.ToString())
                    .Satisfy(meta => meta.Key.IsArray && meta.Key != typeof (byte[]));

            var dictWrite =
                "bw.Write((({2}){0}).Count);\nforeach (var item{4} in ({2}){0}){{{1}{3}}}".Basic<KeyValuePair<Type, string>>(
                        meta => meta.Value
                        ,meta => BaseWriteCoder(countPostfix + 1).Code(new KeyValuePair<Type, string>(meta.Key.GetGenericArguments()[0], string.Format("(item{0}.Key)", countPostfix)))
                        ,meta => GetTypeStr(meta.Key)
                        ,meta => BaseWriteCoder(countPostfix + 1).Code(new KeyValuePair<Type, string>(meta.Key.GetGenericArguments()[1], string.Format("(item{0}.Value)", countPostfix)))
                        ,_ => countPostfix.ToString())
                    .Satisfy(meta => meta.Key.Name == "Dictionary`2");

            return
                Generator.GenSelect(
                    listWrite
                    , arrayWrite
                    , dictWrite
                    , baseWrite);
        }

        public static ICoder<KeyValuePair<Type, string>> BaseReadCoder(int countPostfix=0)
        {
            var baseRead =
                Generator.GenSelect(
                    "br.Read{0}();".Basic<Type>(m => m.Name).Satisfy(m => m.IsPrimitive || m == typeof(string))
                    , "({0})br.ReadInt32();".Basic<Type>(m => m.Name).Satisfy(m => m.IsEnum)
                    , "br.ReadBytes(br.ReadInt32());".Unit<Type>().Satisfy(m => m == typeof(byte[]))
                    , "(new {0}()).Read(br);".Basic<Type>(m => m.Name));

            var listRead =
                    "var count{2} = br.ReadInt32();\nvar listVal{2} = new {0}(count{2});if (count{2} > 0){{for (int i{2} = 0; i{2} < count{2}; i{2}++){{var item{2} = default({3});{1}\nlistVal{2}.Add(item{2});}}}}"
                    .Basic<Type>(
                        GetTypeStr
                        , meta => BaseReadCoder(countPostfix+1).Code(new KeyValuePair<Type, string>(meta.GetGenericArguments()[0], "item"+countPostfix.ToString()))
                        , _ => countPostfix.ToString()
                        , meta=> GetTypeStr(meta.GetGenericArguments()[0]));

            var arrayRead =
                    "var count{2} = br.ReadInt32();\nvar arrayVal{2} = new {0}[count{2}];if (count{2} > 0){{for (int i{2} = 0; i{2} < count{2}; i{2}++){{arrayVal{2}[i{2}] = {1}}}}}"
                    .Basic<Type>(
                        meta=>GetTypeStr(meta.GetElementType())
                        , meta => baseRead.Code(meta.GetElementType())
                        , _ => countPostfix.ToString());

            var dictRead =
        "var count{3} = br.ReadInt32();\nvar dictVal{3} = new {0}(count{3});if (count{3} > 0){{for (int i{3} = 0; i{3} < count{3}; i{3}++){{var key{3} = default({4});\nvar value{3} = default({5});\n{1}\n{2}\ndictVal{3}.Add(key{3}, value{3});}}}}"
        .Basic<Type>(
            GetTypeStr
            , meta => BaseReadCoder(countPostfix+1).Code(new KeyValuePair<Type, string>(meta.GetGenericArguments()[0], "key" + countPostfix.ToString()))
            , meta => BaseReadCoder(countPostfix + 1).Code(new KeyValuePair<Type, string>(meta.GetGenericArguments()[1], "value" + countPostfix.ToString()))
            , _ => countPostfix.ToString()
            , meta=>GetTypeStr(meta.GetGenericArguments()[0])
            , meta => GetTypeStr(meta.GetGenericArguments()[1]));

            return
                Generator.GenSelect(
                    "{{{0}\n{1} = listVal{2};}}".Basic < KeyValuePair < Type, string >>(
                        meta=> listRead.Code(meta.Key)
                        , meta=>meta.Value
                        , _=>countPostfix.ToString()).Satisfy(meta => meta.Key.Name == "List`1")
                    , "{{{0}\n{1} = arrayVal{2};}}".Basic<KeyValuePair<Type, string>>(
                        meta => arrayRead.Code(meta.Key)
                        , meta => meta.Value
                        , _ => countPostfix.ToString()).Satisfy(meta => meta.Key.IsArray && meta.Key != typeof(byte[]))
                    , "{{{0}\n{1} = dictVal{2};}}".Basic<KeyValuePair<Type, string>>(
                        meta => dictRead.Code(meta.Key)
                        , meta => meta.Value
                        , _ => countPostfix.ToString()).Satisfy(meta => meta.Key.Name == "Dictionary`2")
                    , "{0} = ".Basic<KeyValuePair<Type, string>>(meta => meta.Value).Combine(baseRead, meta => meta.Key));
        }

        public static ICoder<Tuple<Type, ICoder<T>, T>> ReadCoder<T>()
        {
            return
                BaseReadCoder()
                    .Lift(
                        (Tuple<Type, ICoder<T>, T> meta) =>
                            new KeyValuePair<Type, string>(meta.Item1, meta.Item2.Code(meta.Item3)));
        }

        public static ICoder<Tuple<Type, ICoder<T>, T>> WriteCoder<T>()
        {
            return
                BaseWriteCoder()
                    .Lift(
                        (Tuple<Type, ICoder<T>, T> meta) =>
                            new KeyValuePair<Type, string>(meta.Item1, meta.Item2.Code(meta.Item3)));
        }

        public static ICoder<Tuple<Type, ICoder<T>, T>> ReadWithCheckCoder<T>()
        {
            return
                Generator.GenSelect(
                    "if (br.ReadByte() == (byte)SerializeObjectMark.Common){{{0}}}".Basic<Tuple<Type, ICoder<T>, T>>(
                        meta => ReadCoder<T>().Code(meta)).Satisfy(meta => !meta.Item1.IsValueType)
                    , ReadCoder<T>());
        }

        public static ICoder<Tuple<Type, ICoder<T>, T>> WriteWithCheckCoder<T>()
        {
            return
                Generator.GenSelect(
                    "if ({0} == null){{bw.Write((byte)SerializeObjectMark.IsNull);}}else{{bw.Write((byte)SerializeObjectMark.Common);{1}}}"
                        .Basic<Tuple<Type, ICoder<T>, T>>(meta => meta.Item2.Code(meta.Item3), WriteCoder<T>().Code)
                        .Satisfy(meta => !meta.Item1.IsValueType)
                    , WriteCoder<T>());
        }
    }
}
