/*
 * Copyright (c) 2000, 2025, Oracle and/or its affiliates.
 *
 * Licensed under the Universal Permissive License v 1.0 as shown at
 * https://oss.oracle.com/licenses/upl.
 */
using System;

using NUnit.Framework;

using Tangosol.IO.Resources;
using Tangosol.Net;
using Tangosol.Run.Xml;
using Tangosol.Util.Filter;
using Tangosol.Util.Processor;

namespace Tangosol.Data
{
    [TestFixture]
    public class PofReferenceTests
    {
        [SetUp]
        public void SetUp()
        {
            TestContext.Error.WriteLine($"[START] {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}: {TestContext.CurrentContext.Test.FullName}");
        }

        [TearDown]
        public void TearDown()
        {
            TestContext.Error.WriteLine($"[END]   {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}: {TestContext.CurrentContext.Test.FullName}");
        }

        /**
        * Test POF object with circular references.
        */
        [Test]
        public void PofCircularReference()
        {
            CacheFactory.DefaultOperationalConfigPath = "//Coherence.Tests/Tangosol.Resources/s4hc-test-coherence.xml";
            CacheFactory.DefaultOperationalConfig = XmlHelper.LoadResource(ResourceLoader.GetResource(
                "assembly://Coherence.Tests/Tangosol.Resources/s4hc-test-coherence.xml"), "Operational configuration");
            CacheFactory.DefaultPofConfigPath = "//Coherence.Tests/Tangosol.Resources/s4hc-test-reference-config.xml";
            CacheFactory.DefaultPofConfig     = XmlHelper.LoadResource(ResourceLoader.GetResource(
                "assembly://Coherence.Tests/Tangosol.Resources/s4hc-test-reference-config.xml"), "POF configuration");
            IConfigurableCacheFactory ccf = CacheFactory.ConfigurableCacheFactory;
            ccf.Config = XmlHelper.LoadXml(
                "assembly://Coherence.Tests/Tangosol.Resources/s4hc-cache-config-reference.xml");
            ICacheService service = (ICacheService) ccf.EnsureService("ExtendTcpCacheService");
            INamedCache   cache   = service.EnsureCache("dist-extend-reference");

            var joe  = new PortablePerson("Joe Smith", new DateTime(78, 4, 25));
            var jane = new PortablePerson("Jane Smith", new DateTime(80, 5, 22));
            joe.Spouse = jane;
            jane.Spouse = joe;

            cache.Add("Key1", joe);
            cache.Invoke("Key1", new ConditionalPut(new AlwaysFilter(), joe, false));

            CacheFactory.DefaultPofConfigPath = "//Coherence.Tests/Tangosol.Resources/s4hc-test-config.xml";
            CacheFactory.DefaultPofConfig     = XmlHelper.LoadResource(ResourceLoader.GetResource(
                "assembly://Coherence.Tests/Tangosol.Resources/s4hc-test-config.xml"), "POF configuration");
            CacheFactory.Shutdown();
        } 
    }
}
