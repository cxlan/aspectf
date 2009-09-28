﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Collections;

namespace OmarALZabir.AspectF
{
    /// <summary>
    /// AspectF
    /// (C) Omar AL Zabir 2009 All rights reserved.
    /// 
    /// AspectF lets you add strongly typed Aspects within you code, 
    /// anywhere in the code, in a fluent way. In common AOP frameworks, 
    /// you define aspects as individual classes and you leave indication 
    /// in the code where the aspect needs to be injected. A weaver 
    /// then weaves it into the code for you. You can also implement AOP
    /// using Attributes and by inheriting your classes from MarshanByRef. 
    /// But that's not an option for you always to do so. There's also 
    /// another way of doing AOP using DynamicProxy.
    /// 
    /// AspectF tries to avoid all these special tricks. It has no need 
    /// for a weaver (or any post build tool). It also does not require
    /// extending classes from MarshalByRef or using DynamicProxy.
    /// 
    /// AspectF offers a plain vanilla way of putting aspects within 
    /// your methods. You can wrap your code using Aspects 
    /// by using standard wellknown C#/VB.NET code. 
    /// </summary>
    public class AspectF
    {
        /// <summary>
        /// Chain of aspects to invoke
        /// </summary>
        internal Action<Action> Chain = null;

        /// <summary>
        /// The acrual work delegate that is finally called
        /// </summary>
        internal Delegate WorkDelegate;

        /// <summary>
        /// Create a composition of function e.g. f(g(x))
        /// </summary>
        /// <param name="newAspectDelegate">A delegate that offers an aspect's behavior. 
        /// It's added into the aspect chain</param>
        /// <returns></returns>
        [DebuggerStepThrough]
        public AspectF Combine(Action<Action> newAspectDelegate)
        {
            if (this.Chain == null)
            {
                this.Chain = newAspectDelegate;
            }
            else
            {
                Action<Action> existingChain = this.Chain;
                Action<Action> callAnother = (work) => existingChain(() => newAspectDelegate(work));
                this.Chain = callAnother;
            }
            return this;
        }

        /// <summary>
        /// Execute your real code applying the aspects over it
        /// </summary>
        /// <param name="work">The actual code that needs to be run</param>
        [DebuggerStepThrough]
        public void Do(Action work)
        {
            if (this.Chain == null)
            {
                work();
            }
            else
            {
                this.Chain(work);
            }
        }

        /// <summary>
        /// Execute your real code applying aspects over it.
        /// </summary>
        /// <typeparam name="TReturnType"></typeparam>
        /// <param name="work">The actual code that needs to be run</param>
        /// <returns></returns>
        [DebuggerStepThrough]
        public TReturnType Return<TReturnType>(Func<TReturnType> work)
        {
            this.WorkDelegate = work;

            if (this.Chain == null)
            {
                return work();
            }
            else
            {
                TReturnType returnValue = default(TReturnType);
                this.Chain(() =>
                {
                    Func<TReturnType> workDelegate = WorkDelegate as Func<TReturnType>;
                    returnValue = workDelegate();
                });
                return returnValue;
            }            
        }
        
        /// <summary>
        /// Handy property to start writing aspects using fluent style
        /// </summary>
        public static AspectF Define
        {
            [DebuggerStepThrough]
            get
            {
                return new AspectF();
            }
        }
    }   
    
    public static class AspectExtensions
    {
        [DebuggerStepThrough]
        public static void DoNothing()
        {
        }

        [DebuggerStepThrough]
        public static void DoNothing(params object[] whatever)
        {
        }

        [DebuggerStepThrough]
        public static AspectF Retry(this AspectF aspects, ILogger logger)
        {
            return aspects.Combine((work) =>
                Retry(1000, 1, (error) => DoNothing(error), x => DoNothing(), work, logger));
        }

        [DebuggerStepThrough]
        public static AspectF Retry(this AspectF aspects, Action<IEnumerable<Exception>> failHandler, ILogger logger)
        {
            return aspects.Combine((work) =>
                Retry(1000, 1, (error) => DoNothing(error), x => DoNothing(), work, logger));
        }

        [DebuggerStepThrough]
        public static AspectF Retry(this AspectF aspects, int retryDuration, ILogger logger)
        {
            return aspects.Combine((work) =>
                Retry(retryDuration, 1, (error) => DoNothing(error), x => DoNothing(), work, logger));
        }

        [DebuggerStepThrough]
        public static AspectF Retry(this AspectF aspects, int retryDuration,
            Action<Exception> errorHandler, ILogger logger)
        {
            return aspects.Combine((work) =>
                Retry(retryDuration, 1, errorHandler, x => DoNothing(), work, logger));
        }

        [DebuggerStepThrough]
        public static AspectF Retry(this AspectF aspects, int retryDuration,
            int retryCount, Action<Exception> errorHandler, ILogger logger)
        {
            return aspects.Combine((work) =>
                Retry(retryDuration, retryCount, errorHandler, x => DoNothing(), work, logger));
        }

        [DebuggerStepThrough]
        public static AspectF Retry(this AspectF aspects, int retryDuration,
            int retryCount, Action<Exception> errorHandler, Action<IEnumerable<Exception>> retryFailed, ILogger logger)
        {
            return aspects.Combine((work) => 
                Retry(retryDuration, retryCount, errorHandler, retryFailed, work, logger));
        }

        [DebuggerStepThrough]
        public static void Retry(int retryDuration, int retryCount,
            Action<Exception> errorHandler, Action<IEnumerable<Exception>> retryFailed, Action work, ILogger logger)
        {
            List<Exception> errors = null;
            do
            {
                try
                {
                    work();
                    return;
                }
                catch (Exception x)
                {
                    if (null == errors)
                        errors = new List<Exception>();
                    errors.Add(x);
                    logger.LogException(x);
                    errorHandler(x);
                    System.Threading.Thread.Sleep(retryDuration);
                }
            } while (retryCount-- > 0);
            retryFailed(errors);
        }

        [DebuggerStepThrough]
        public static AspectF Delay(this AspectF aspect, int milliseconds)
        {
            return aspect.Combine((work) =>
            {
                System.Threading.Thread.Sleep(milliseconds);
                work();
            });
        }

        [DebuggerStepThrough]
        public static AspectF MustBeNonDefault<T>(this AspectF aspect, params T[] args)
            where T:IComparable
        {
            return aspect.Combine((work) =>
            {
                T defaultvalue = default(T);
                for (int i = 0; i < args.Length; i++)
                {
                    T arg = args[i];
                    if (arg == null || arg.Equals(defaultvalue))
                        throw new ArgumentException(
                            string.Format("Parameter at index {0} is null", i));
                }

                work();
            });
        }

        [DebuggerStepThrough]
        public static AspectF MustBeNonNull(this AspectF aspect, params object[] args)
        {
            return aspect.Combine((work) =>
            {
                for (int i = 0; i < args.Length; i++)
                {
                    object arg = args[i];
                    if (arg == null)
                        throw new ArgumentException(
                            string.Format("Parameter at index {0} is null", i));
                }

                work();
            });
        }

        [DebuggerStepThrough]
        public static AspectF Until(this AspectF aspect, Func<bool> test)
        {
            return aspect.Combine((work) =>
            {
                while (!test()) ;
                work();
            });
        }

        [DebuggerStepThrough]
        public static AspectF While(this AspectF aspect, Func<bool> test)
        {
            return aspect.Combine((work) =>
            {
                while (test()) ;
                work();
            });
        }

        [DebuggerStepThrough]
        public static AspectF WhenTrue(this AspectF aspect, params Func<bool>[] conditions)
        {
            return aspect.Combine((work) =>
            {
                foreach (Func<bool> condition in conditions)
                    if (!condition())
                        return;

                work();
            });
        }

        [DebuggerStepThrough]
        public static AspectF Log(this AspectF aspect, ILogger logger, string[] categories,
            string logMessage, params object[] arg)
        {
            return aspect.Combine((work) =>
            {
                logger.Log(categories, logMessage);

                work();
            });
        }

        [DebuggerStepThrough]
        public static AspectF Log(this AspectF aspect, ILogger logger, 
            string logMessage, params object[] arg)
        {
            return aspect.Combine((work) =>
            {
                logger.Log(string.Format(logMessage,arg));

                work();
            });
        }


        [DebuggerStepThrough]
        public static AspectF Log(this AspectF aspect, ILogger logger, string[] categories,
            string beforeMessage, string afterMessage)
        {
            return aspect.Combine((work) =>
            {
                logger.Log(categories, beforeMessage);

                work();

                logger.Log(categories, afterMessage);
            });
        }

        [DebuggerStepThrough]
        public static AspectF Log(this AspectF aspect, ILogger logger, 
            string beforeMessage, string afterMessage)
        {
            return aspect.Combine((work) =>
            {
                logger.Log(beforeMessage);

                work();

                logger.Log(afterMessage);
            });
        }

        [DebuggerStepThrough]
        public static AspectF HowLong(this AspectF aspect, ILogger logger, 
            string startMessage, string endMessage)
        {
            return aspect.Combine((work) =>
            {
                DateTime start = DateTime.Now;
                logger.Log(startMessage);

                work();

                DateTime end = DateTime.Now.ToUniversalTime();
                TimeSpan duration = end - start;

                logger.Log(string.Format(endMessage, duration.TotalMilliseconds, 
                    duration.TotalSeconds, duration.TotalMinutes, duration.TotalHours,
                    duration.TotalDays));
            });
        }

        [DebuggerStepThrough]
        public static AspectF TrapLog(this AspectF aspect, ILogger logger)
        {
            return aspect.Combine((work) =>
            {
                try
                {
                    work();
                }
                catch (Exception x)
                {
                    logger.LogException(x);
                }
            });
        }

        [DebuggerStepThrough]
        public static AspectF TrapLogThrow(this AspectF aspect, ILogger logger)
        {
            return aspect.Combine((work) =>
            {
                try
                {
                    work();
                }
                catch (Exception x)
                {
                    logger.LogException(x);
                    throw;
                }
            });
        }

        [DebuggerStepThrough]
        public static AspectF RunAsync(this AspectF aspect, Action completeCallback)
        {
            return aspect.Combine((work) => work.BeginInvoke(asyncresult => 
                { 
                    work.EndInvoke(asyncresult); completeCallback(); 
                }, null));
        }

        [DebuggerStepThrough]
        public static AspectF RunAsync(this AspectF aspect)
        {
            return aspect.Combine((work) => work.BeginInvoke(asyncresult => 
                { 
                    work.EndInvoke(asyncresult); 
                }, null));
        }

        public static AspectF Cache<TReturnType>(this AspectF aspect, 
            ICacheResolver cacheResolver, string key)
        {            
            return aspect.Combine((work) => 
            {
                Cache<TReturnType>(aspect, cacheResolver, key, work);
            });
        }

        public static AspectF CacheList<TReturnType>(this AspectF aspect,
            ICacheResolver cacheResolver, string listCacheKey, Func<TReturnType, string> getItemKey)
        {
            return aspect.Combine((work) =>
            {
                Func<IEnumerable<TReturnType>> workDelegate = aspect.WorkDelegate as Func<IEnumerable<TReturnType>>;
                Func<IEnumerable<TReturnType>> newWorkDelegate = () =>
                {
                    IEnumerable<TReturnType> collection = workDelegate();
                    foreach (TReturnType item in collection)
                    {
                        string key = getItemKey(item);
                        cacheResolver.Set(key, item);
                    }
                    return collection;
                };
                aspect.WorkDelegate = newWorkDelegate;

                Cache<IEnumerable<TReturnType>>(aspect, cacheResolver, listCacheKey, work);
            });
        }

        public static AspectF CacheRetry<TReturnType>(this AspectF aspect,
            ICacheResolver cacheResolver, 
            ILogger logger,
            string key)
        {
            return aspect.Combine((work) =>
            {
                try
                {
                    Cache<TReturnType>(aspect, cacheResolver, key, work);
                }
                catch (Exception x)
                {
                    logger.LogException(x);
                    System.Threading.Thread.Sleep(1000);

                    //Retry
                    try
                    {
                        Cache<TReturnType>(aspect, cacheResolver, key, work);
                    }
                    catch (Exception ex)
                    {
                        logger.LogException(ex);
                        throw ex;
                    }
                }
            });
        }

        private static void Cache<TReturnType>(AspectF aspect, ICacheResolver cacheResolver, string key, Action work)             
        {
            object cachedData = cacheResolver.Get(key);
            if (cachedData == null)
            {
                Func<TReturnType> workDelegate = aspect.WorkDelegate as Func<TReturnType>;
                TReturnType realObject = workDelegate();
                cacheResolver.Add(key, realObject);
                workDelegate = () => realObject;
                aspect.WorkDelegate = workDelegate;
            }
            else
            {
                aspect.WorkDelegate = new Func<TReturnType>(() => (TReturnType)cachedData);
            }

            work();
        }


    }
}