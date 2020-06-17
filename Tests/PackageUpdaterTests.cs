using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.TestTools;

namespace PackageUpdater.Tests
{
    public class PackageUpdaterTests
    {
        [UnityTest]
        public IEnumerator AllPackagesShouldBeUpToDate()
        {
            var listRequest = Client.List();
            yield return new WaitUntil(() => listRequest.IsCompleted);

            Assert.IsNotNull(listRequest.Result);

            var availablePackages = UnityPackageUpdater.PackagesFetched(listRequest.Result);
            if (availablePackages.Count > 0)
            {
                var packagesAvailable = availablePackages.Select(p => p.Key.PackageName);
                Assert.IsEmpty(packagesAvailable);
            }
        }
    }
}