﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace NoRM.BSON
{
    /// <summary>
    /// Provides a mechanism for adding and removing arbitrary properties on objects.
    /// </summary>
    public static class ExpandoProps
    {
        private static Dictionary<WeakReference, Flyweight> _expandoProps = new Dictionary<WeakReference, Flyweight>(0);
        private static ReaderWriterLock _dictionaryLock = new ReaderWriterLock();
        private static String _lockToken = "LOCK_THREAD";
        private static Thread _scrubExpandos;

        public static Flyweight FlyweightForObject<T>(T document)
        {
            Flyweight retval = null;
            ExpandoProps._dictionaryLock.AcquireReaderLock(30000);
            var p = ExpandoProps._expandoProps.FirstOrDefault(y => y.Key.Target == (object)document);
            if (p.Value != null)
            {
                retval = p.Value;
            }
            ExpandoProps._dictionaryLock.ReleaseReaderLock();
            return retval;
        }

        /// <summary>
        /// Set the deserialized props for the specified object into a global cache, to be cleared every 30 seconds.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <param name="addedProps"></param>
        public static void SetFlyWeightObjects(IDictionary<WeakReference, Flyweight> props)
        {
            #region Initialize expando dictionary and the clean-up thread.
            if (ExpandoProps._scrubExpandos == null)
            {
                lock (ExpandoProps._lockToken)
                {
                    if (ExpandoProps._scrubExpandos == null)
                    {
                        ExpandoProps._scrubExpandos = new Thread(() =>
                        {
                            while (true)
                            {
                                ExpandoProps._dictionaryLock.AcquireWriterLock(30000);
                                //trim the dictionary of anything where the object has been collected.
                                ExpandoProps._expandoProps = new Dictionary<WeakReference, Flyweight>(
                                                        ExpandoProps._expandoProps.Where(y => y.Key.IsAlive)
                                    .ToDictionary(j => j.Key, k => k.Value));
                                ExpandoProps._dictionaryLock.ReleaseWriterLock();

                                //wait 15 seconds before attempting to clear again.
                                Thread.Sleep(15000);
                            }
                        });
                        ExpandoProps._scrubExpandos.IsBackground = true;

                        ExpandoProps._scrubExpandos.Start();

                    }
                }
            }
            #endregion
            
            ExpandoProps._dictionaryLock.AcquireWriterLock(30000);
            foreach (var a in props)
            {
                ExpandoProps._expandoProps[a.Key] = a.Value;
            }
            ExpandoProps._dictionaryLock.ReleaseWriterLock();
        }

        /// <summary>
        /// Set a property on the specified flyweight
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <param name="propertyName"></param>
        /// <param name="property"></param>
        public static void Set<T>(this IFlyweight obj, String propertyName, T property)
        {
            ExpandoProps._dictionaryLock.AcquireWriterLock(30000);
            var dict = ExpandoProps._expandoProps.FirstOrDefault(y => y.Key.Target == (object)obj);

            if (dict.Key == null)
            {
                var reference = new WeakReference(obj);
                ExpandoProps._expandoProps[reference] = new Flyweight();
            }
            ExpandoProps._expandoProps[dict.Key][propertyName] = property;
            ExpandoProps._dictionaryLock.ReleaseLock();
        }

        /// <summary>
        /// Remove a property from the dictionary.
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="propertyName"></param>
        /// <returns>True if the property was found, false otherwise.</returns>
        public static bool DeleteProperty(this IFlyweight obj, String propertyName)
        {
            bool retval = false;
            ExpandoProps._dictionaryLock.AcquireWriterLock(30000);
            var dict = ExpandoProps._expandoProps.FirstOrDefault(y => y.Key.Target == (object)obj);
            dict.Value.DeleteProperty(propertyName);
            ExpandoProps._dictionaryLock.ReleaseWriterLock();
            return retval;
        }

        public static IEnumerable<ExpandoProperty> AllProperties(this IFlyweight obj)
        {
            var retval = Enumerable.Empty<ExpandoProperty>();
            
            ExpandoProps._dictionaryLock.AcquireReaderLock(30000);
            var dict = ExpandoProps._expandoProps.FirstOrDefault(y => y.Key.Target == (object)obj);
            if (dict.Key != null && dict.Value != null)
            {
                retval = dict.Value.AllProperties().ToArray();
            }
            ExpandoProps._dictionaryLock.ReleaseReaderLock();
            return retval;
        }

        /// <summary>
        /// Provides a lookup for a particular property.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public static T Get<T>(this IFlyweight obj, String propertyName) where T : class
        {
            T retval = null;

            ExpandoProps._dictionaryLock.AcquireReaderLock(30000);
            var dict = ExpandoProps._expandoProps.FirstOrDefault(y => y.Key.Target == (object)obj);
            if (dict.Key != null && dict.Value != null)
            {
                var value = dict.Value.Get<T>(propertyName);
            }

            ExpandoProps._dictionaryLock.ReleaseReaderLock();
            return retval;
        }

    }
}
