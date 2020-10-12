using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using NUnit.Framework;
using Tangosol.Net;

namespace Tangosol.Linq
{
    [TestFixture]
    public class LinqTest
    {
        NameValueCollection appSettings = TestUtils.AppSettings;

        private string CacheName
        {
            get { return appSettings.Get("cacheName"); }
        }

        [Test]
        public void TestSelect()
        {
            INamedCache cache = CacheFactory.GetCache(CacheName);
            cache.Clear();

            PortablePerson original = new PortablePerson();
            original.Name = "Aleksandar Seovic";
            original.Address = new Address("street", "Belgrade", "SRB", "11000");
            cache.Insert("p1", original);

            var query = from pn in cache.AsQueriable<PortablePerson>()
                select pn;

            var persons = query.ToList();
            Assert.AreEqual(original, persons[0]);

            CacheFactory.Shutdown();
        }

        [Test]
        public void TestEmptyCacheAggregators()
        {
            INamedCache cache = CacheFactory.GetCache(CacheName);
            cache.Clear();

            var query = from scores in cache.AsQueriable<Score>()
                select scores.LongValue;

            int count = query.Count();
            Assert.AreEqual(0, count);

            Assert.Throws(typeof(InvalidOperationException), delegate { query.Sum(); });

            Assert.Throws(typeof(InvalidOperationException), delegate { query.Min(); });

            Assert.Throws(typeof(InvalidOperationException), delegate { query.Max(); });

            Assert.Throws(typeof(InvalidOperationException), delegate { query.Average(); });

            var distinctScores = query.Distinct();
            CollectionAssert.IsEmpty(distinctScores);
            CacheFactory.Shutdown();
        }

        [Test]
        public void TestDefaultValue()
        {
            INamedCache cache = CacheFactory.GetCache(CacheName);
            cache.Clear();

            var query = from scores in cache.AsQueriable<Score>()
                select scores.LongValue;

            int count = query.DefaultIfEmpty(5).Count();
            Assert.AreEqual(0, count);

            long sum = query.DefaultIfEmpty().Sum();
            Assert.AreEqual(0, sum);

            sum = query.DefaultIfEmpty(8).Sum();
            Assert.AreEqual(8, sum);

            long min = query.DefaultIfEmpty().Min();
            Assert.AreEqual(0, min);

             min = query.DefaultIfEmpty(-9).Min();
            Assert.AreEqual(-9, min);

            long max = query.DefaultIfEmpty().Max();
            Assert.AreEqual(0, max);

            max = query.DefaultIfEmpty(11).Max();
            Assert.AreEqual(11, max);

            double avg = query.DefaultIfEmpty().Average();
            Assert.AreEqual(0, avg);

            avg = query.DefaultIfEmpty(33).Average();
            Assert.AreEqual(33, avg);

            var distinctScores = query.DefaultIfEmpty(7).Distinct();
            CollectionAssert.IsEmpty(distinctScores);

            CacheFactory.Shutdown();
        }

        [Test]
        public void TestLongPropertyAggregators()
        {
            INamedCache cache = CacheFactory.GetCache(CacheName);
            cache.Clear();
            Hashtable scs = new Hashtable();
            for (int i = 1; i <= 10; i++)
            {
                Score score = new Score {LongValue = Convert.ToInt64(i * 1000)};
                scs.Add(i, score);
            }

            {
                Score score = new Score {LongValue = Convert.ToInt64(5 * 1000)};
                scs.Add(11, score);
            }

            cache.InsertAll(scs);

            var query = from scores in cache.AsQueriable<Score>()
                select scores.LongValue;

            int count = query.Count();
            Assert.AreEqual(11, count);

            long sum = query.Sum();
            Assert.AreEqual(60000, sum);

            long min = query.Min();
            Assert.AreEqual(1000, min);

            long max = query.Max();
            Assert.AreEqual(10000, max);

            double avg = query.Average();
            Assert.AreEqual(5454.545454545455d, avg);

            var distinctScores = query.Distinct().ToList();
            Assert.AreEqual(10, distinctScores.Count);

            CacheFactory.Shutdown();
        }

        [Test]
        public void TestDoublePropertyAggregators()
        {
            INamedCache cache = CacheFactory.GetCache(CacheName);
            cache.Clear();
            Hashtable scs = new Hashtable();
            for (int i = 1; i <= 10; i++)
            {
                Score score = new Score {DoubleValue = Convert.ToDouble(i * 1000.33)};
                scs.Add(i, score);
            }

            {
                Score score = new Score {DoubleValue = Convert.ToDouble(5 * 1000.33)};
                scs.Add(11, score);
            }
            cache.InsertAll(scs);

            var query = from scores in cache.AsQueriable<Score>()
                select scores.DoubleValue;

            int count = query.Count();
            Assert.AreEqual(11, count);

            double sum = query.Sum();
            Assert.AreEqual(60019.80000000001, sum);


            double min = query.Min();
            Assert.AreEqual(1000.33d, min);

            double max = query.Max();
            Assert.AreEqual(10003.300000000001d, max);

            double avg = query.Average();
            Assert.AreEqual(5456.3454545454551d, avg);

            var distinctScores = query.Distinct().ToList();
            Assert.AreEqual(10, distinctScores.Count);

            CacheFactory.Shutdown();
        }

        [Test]
        public void TestExtractor()
        {
            INamedCache cache = CacheFactory.GetCache(CacheName);
            cache.Clear();

            var person1 = new PortablePerson
            {
                Name = "Person 1",
                DOB = new DateTime(1900, 4, 5),
            };

            var person2 = new PortablePerson
            {
                Name = "Person 2",
                DOB = new DateTime(1901, 11, 15),
            };

            cache.Insert(person1.Name, person1);
            cache.Insert(person2.Name, person2);

            var query = from p in cache.AsQueriable<Person>()
                select p.Name;
            ISet<string> names = query.ToHashSet();
            var expectedNames = new HashSet<string>()
            {
                "Person 1",
                "Person 2"
            };
            CollectionAssert.AreEquivalent(expectedNames, names);

            CacheFactory.Shutdown();
        }

        [Test]
        public void TestNestedExtractor()
        {
            INamedCache cache = CacheFactory.GetCache(CacheName);
            cache.Clear();

            var person1 = new PortablePerson
            {
                Name = "Person 1",
                DOB = new DateTime(1900, 4, 5),
                Address = new Address("Street A", "City A", "State A", "11111")
            };

            var person2 = new PortablePerson
            {
                Name = "Person 2",
                DOB = new DateTime(1901, 11, 15),
            };

            cache.Insert(person1.Name, person1);
            cache.Insert(person2.Name, person2);

            var query = from p in cache.AsQueriable<Person>()
                select p.Address.Street;

            CollectionAssert.AreEquivalent(new[] {null, "Street A"}, query.ToArray());

            CacheFactory.Shutdown();
        }

        [Test]
        public void TestWhereClause()
        {
            INamedCache cache = CacheFactory.GetCache(CacheName);
            cache.Clear();

            var person1 = new PortablePerson
            {
                Name = "Person 1",
                DOB = new DateTime(1900, 4, 5),
                Address = new Address("Street A", "City A", "State A", "11111")
            };

            var person2 = new PortablePerson
            {
                Name = "Person 2",
                DOB = new DateTime(1901, 11, 15),
                Address = new Address("Street B", "City B", "State B", "22222")
            };
            var person3 = new PortablePerson
            {
                Name = "Person 3",
                DOB = new DateTime(1902, 4, 5),
                Address = new Address("Street C", "City C", "State C", "33333")
            };

            var person4 = new PortablePerson
            {
                Name = "Person 4",
                DOB = new DateTime(1903, 11, 15),
                Address = new Address("Street D", "City D", "State D", "4444")
            };

            cache.Insert(person1.Name, person1);
            cache.Insert(person2.Name, person2);
            cache.Insert(person3.Name, person3);
            cache.Insert(person4.Name, person4);

            var query = from p in cache.AsQueriable<Person>()
                where p.DOB == new DateTime(1901, 11, 15)
                select p;
            var persons = query.ToList();
            Assert.AreEqual(person2, persons[0]);

            query = from p in cache.AsQueriable<Person>()
                where p.DOB > new DateTime(1900, 4, 5)
                where p.DOB < new DateTime(1903, 11, 15)
                select p;
            CollectionAssert.AreEquivalent(new[] {person2, person3}, query.ToList());

            query = from p in cache.AsQueriable<Person>()
                where p.DOB >= new DateTime(1901, 11, 15)
                where p.DOB <= new DateTime(1902, 4, 5)
                select p;
            CollectionAssert.AreEquivalent(new[] {person2, person3}, query.ToList());

            query = from p in cache.AsQueriable<Person>()
                where p.DOB >= new DateTime(1901, 11, 15)
                      && p.DOB <= new DateTime(1902, 4, 5)
                select p;
            CollectionAssert.AreEquivalent(new[] {person2, person3}, query.ToList());

            query = from p in cache.AsQueriable<Person>()
                where p.DOB == new DateTime(1900, 4, 5)
                      || p.DOB == new DateTime(1903, 11, 15)
                select p;
            CollectionAssert.AreEquivalent(new[] {person1, person4}, query.ToList());

            query = from p in cache.AsQueriable<Person>()
                where p.DOB != new DateTime(1900, 4, 5)
                select p;
            CollectionAssert.AreEquivalent(new[] {person2, person3, person4}, query.ToList());

            query = from p in cache.AsQueriable<Person>()
                where p.Address.Street == "Street D"
                select p;
            CollectionAssert.AreEquivalent(new[] {person4}, query.ToList());

            CacheFactory.Shutdown();
        }
    }
}