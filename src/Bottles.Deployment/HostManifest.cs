using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
using Bottles.Deployment.Runtime;
using FubuCore.Binding;
using FubuCore.Configuration;
using System.Linq;
using FubuCore;
using FubuCore.Reflection;

namespace Bottles.Deployment
{
    public class HostManifest
    {
        private static readonly IObjectResolver _resolver = ObjectResolver.Basic();
        private readonly IList<BottleReference> _bottles = new List<BottleReference>();
        private readonly IList<SettingsData> _data = new List<SettingsData>();


        public HostManifest(string name)
        {
            Name = name;
        }

        public T GetDirective<T>() where T : class, new()
        {
            var provider = new SettingsProvider(_resolver, _data);
            return provider.SettingsFor<T>();
        }

        public IEnumerable<SettingDataSource> CreateDiagnosticReport()
        {
            return new SettingsProvider(_resolver, _data).CreateDiagnosticReport();
        }

        public IDirective GetDirective(Type directiveType)
        {
            var provider = new SettingsProvider(_resolver, _data);
            return (IDirective) provider.SettingsFor(directiveType);
        }

        public string Name { get; private set; }
    
        public void RegisterBottle(BottleReference reference)
        {
            _bottles.Add(reference);
        }

        public IEnumerable<BottleReference> BottleReferences
        {
            get { return _bottles; }
        }

        public void RegisterSettings(SettingsData data)
        {
            _data.Add(data);
        }

        public void RegisterValue<T>(Expression<Func<T, object>> expression, object value) where T : IDirective
        {
            var key = "{0}.{1}".ToFormat(typeof (T).Name, expression.ToAccessor().PropertyNames.Join("."));
            var data = new SettingsData(SettingCategory.core).With(key, value.ToString());
            
            RegisterSettings(data);
        }

        public void Append(HostManifest otherHost)
        {
            _bottles.Fill(otherHost._bottles);
            _data.AddRange(otherHost._data);
        }

        /// <summary>
        /// This is only used for testing
        /// </summary>
        /// <returns></returns>
        public IEnumerable<SettingsData> AllSettingsData()
        {
            return _data;
        }

        public void RegisterBottles(IEnumerable<BottleReference> references)
        {
            _bottles.AddRange(references);
        }


        public IEnumerable<string> UniqueDirectiveNames()
        {
            return _data.SelectMany(x => x.AllKeys)
                .Select(x => x.Split('.')
                .First())
                .Distinct();
        }

        // overridden in testing classes
        public virtual IEnumerable<IDirective> BuildDirectives(IDirectiveTypeRegistry typeRegistry)
        {
            return UniqueDirectiveNames().Select(name =>
            {
                var type = typeRegistry.DirectiveTypeFor(name);
                return GetDirective(type);
            });
        }

        public override string ToString()
        {
            return Name;
        }

        public bool HasBottle(string bottle)
        {
            return _bottles.Any(br => br.Name.Equals(bottle));
        }

    }
}