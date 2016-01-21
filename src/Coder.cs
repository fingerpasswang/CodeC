using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace CodeC
{    
    public interface ICoder<in T> 
    {
        string Code(T meta);
    }

    internal class UnitCoder<T> : ICoder<T>
    {
        readonly string output;
        public UnitCoder(string output)
        {
            this.output = output;
        }

        public string Code(T meta)
        {
            return output;
        }
    }

    internal class ZeroCoder<T> : ICoder<T>
    {
        private static ZeroCoder<T> instance;
        public static ZeroCoder<T> Instance
        {
            get { return instance ?? (instance = new ZeroCoder<T>()); }
        }
        public string Code(T meta)
        {
            return "";
        }
    }

    internal class SatisfyCoder<T> : ICoder<T>
    {
        private readonly Func<T, bool> pred;
        private readonly ICoder<T> coder; 
        public SatisfyCoder(ICoder<T> coder, Func<T, bool> pred)
        {
            this.pred = pred;
            this.coder = coder;
        }

        public string Code(T meta)
        {
            try
            {
                return pred(meta) ? coder.Code(meta) : ZeroCoder<T>.Instance.Code(meta);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }

    internal class SelectCoder<T> : ICoder<T>
    {
        private readonly IEnumerable<ICoder<T>> coders;
        public SelectCoder(IEnumerable<ICoder<T>> coders)
        {
            this.coders = coders;
        }

        public string Code(T meta)
        {
            try
            {
                return coders.Select(coder => coder.Code(meta)).FirstOrDefault(str => !string.IsNullOrEmpty(str));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }

    internal class RepeatedCoder<T> : ICoder<IEnumerable<T>>
    {
        private readonly ICoder<T> coder;
        private readonly string seperator;
        private readonly Func<T, bool> predicate;
        public RepeatedCoder(ICoder<T> coder, string seperator, Func<T, bool> predicate)
        {
            this.coder = coder;
            this.seperator = seperator;
            this.predicate = predicate;
        }

        public string Code(IEnumerable<T> meta)
        {
            try
            {
                bool first = true;
                return meta.Where(m => predicate(m)).Select(coder.Code).Aggregate("", (val, cur) =>
                {
                    if (first)
                    {
                        first = false;
                        return val + cur;
                    }
                    return val + seperator + cur;
                });
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }

    internal class SequenceCoder<T> : ICoder<T> 
    {
        readonly Func<T, string>[] binders;

        public SequenceCoder(params Func<T, string>[] binders)
        {
            this.binders = binders;
        }

        public string Code(T meta)
        {
            try
            {
                return string.Join("", binders.Select(binder => binder(meta)));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }

    internal class BasicCoder<T> : ICoder<T> 
    {
        private readonly Func<T, string> func;

        public BasicCoder(Func<T, string> func)
        {
            this.func = func;
        }

        public string Code(T meta)
        {
            try
            {
                return func(meta);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }
}
