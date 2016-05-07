﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CKAN;
using CKAN.Factorio.Relationships;
using NUnit.Framework;
using Tests.Core;
using Tests.Data;
using ModuleInstaller = CKAN.ModuleInstaller;

namespace Tests.GUI
{
    [TestFixture]
    public class MainModListTests
    {
        [Test]
        public void OnCreation_HasDefaultFilters()
        {
            var item = new MainModList(delegate { }, delegate { return null; });
            Assert.AreEqual(GUIModFilter.Compatible, item.ModFilter, "ModFilter");
            Assert.AreEqual(String.Empty, item.ModNameFilter, "ModNameFilter");
        }

        [Test]
        public void OnModTextFilterChanges_CallsEventHandler()
        {
            var called_n = 0;
            var item = new MainModList(delegate { called_n++; }, delegate { return null; });
            Assert.That(called_n == 1);
            item.ModNameFilter = "randomString";
            Assert.That(called_n == 2);
            item.ModNameFilter = "randomString";
            Assert.That(called_n == 2);
        }
        [Test]
        public void OnModTypeFilterChanges_CallsEventHandler()
        {
            var called_n = 0;
            var item = new MainModList(delegate { called_n++; }, delegate { return null; });
            Assert.That(called_n == 1);
            item.ModFilter = GUIModFilter.Installed;
            Assert.That(called_n == 2);
            item.ModFilter = GUIModFilter.Installed;
            Assert.That(called_n == 2);
        }

        [Test]
        public void ComputeChangeSetFromModList_WithEmptyList_HasEmptyChangeSet()
        {
            var item = new MainModList(delegate { }, delegate { return null; });
            Assert.That(item.ComputeUserChangeSet(), Is.Empty);
        }

        [Test]
        public void IsVisible_WithAllAndNoNameFilter_ReturnsTrueForCompatible()
        {
            using (var tidy = new DisposableKSP())
            {
                using (
                    KSPManager manager = new KSPManager(new NullUser(), new FakeWin32Registry(tidy.KSP))
                    {
                        CurrentInstance = tidy.KSP
                    })
                {
                    var ckan_mod = TestData.RsoModule();
                    var registry = Registry.Empty();
                    registry.AddAvailable(ckan_mod);
                    var item = new MainModList(delegate { }, null);
                    Assert.That(item.IsVisible(new GUIMod(ckan_mod, registry, manager.CurrentInstance.Version())));
                }
            }
        }

        [Test]
        public void CountModsByFilter_EmptyModList_ReturnsZeroForAllFilters()
        {
            var item = new MainModList(delegate { }, null);
            foreach (GUIModFilter filter in Enum.GetValues(typeof(GUIModFilter)))
            {
                Assert.That(item.CountModsByFilter(filter), Is.EqualTo(0));
            }

        }

        [Test]
        [Category("Display")]
        public void ConstructModList_NumberOfRows_IsEqualToNumberOfMods()
        {
            using (var tidy = new DisposableKSP())
            {
                using (
                    KSPManager manager = new KSPManager(new NullUser(), new FakeWin32Registry(tidy.KSP))
                    {
                        CurrentInstance = tidy.KSP
                    })
                {
                    var registry = Registry.Empty();
                    registry.AddAvailable(TestData.RsoModule());
                    registry.AddAvailable(TestData.kOS_014_module());
                    var main_mod_list = new MainModList(null, null);
                    var mod_list = main_mod_list.ConstructModList(new List<GUIMod>
                    {
                        new GUIMod(TestData.RsoModule(), registry, manager.CurrentInstance.Version()),
                        new GUIMod(TestData.kOS_014_module(), registry, manager.CurrentInstance.Version())
                    });
                    Assert.That(mod_list, Has.Count.EqualTo(2));
                }
            }
        }

    }
}
