using MICore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.MIDebugEngine
{
    public class RegisterGroup
    {
        public readonly string Name;
        internal int Count { get; set; }

        public RegisterGroup(string name)
        {
            Name = name;
            Count = 0;
        }
    }

    public class RegisterDescription
    {
        public readonly string Name;
        public RegisterGroup Group { get; set; }
        public readonly int Index;

        public RegisterDescription(string name, RegisterGroup group, int index)
        {
            Name = name;
            Group = group;
            Index = index;
            Group.Count++;
        }
    }

    public class Register
    {
        public RegisterDescription Description { get; private set; }
        public readonly string Content;

        public Register(string content, RegisterDescription description)
        {
            Content = content;
            Description = description;
        }
    }

    /// <summary>
    /// A process's registers. 
    /// 
    /// MI thinks of the register collection as a list of names and a list of index-value tuples. 
    /// -data-list register-names
    ///     registers-names=["r0","r1","r2"]
    /// -data-list-register-values
    ///     register-values=[{number="0",value="..."},{number="1",value="..."},{number="2",value="..."}]
    /// 
    /// AD7 thinks of the register collection as groups of DEBUG_PROPERTY_INFO structures,
    /// Groups such as cpu registers, floating point registers, segment registers, etc.
    ///
    /// So, to keep everybody happy, we have to track the MI index per register, but seperate registers by group.
    /// Register map exists for this purpose
    /// </summary>
    internal class RegisterCollection
    {
        public RegisterCollection(DebuggedProcess process)
        {
            _process = process;
        }

        private async void Initialize()
        {
            if (_descriptions != null)
                return; // already intialized

            string[] names = await _process.MICommandFactory.DataListRegisterNames();

            if (_descriptions != null)
                return; // already initialized

            var nameMap = RegisterNameMap.Create(names);
            var descriptions = new List<RegisterDescription>();
            var groups = new List<RegisterGroup>();
            for (int i = 0; i < names.Length; i++)
            {
                if (String.IsNullOrEmpty(names[i]))
                {
                    continue;
                }
                RegisterGroup group = GetGroupForRegister(groups, names[i], nameMap);
                descriptions.Add(new RegisterDescription(names[i], group, i));
            }
            _groups = groups.AsReadOnly();
            _descriptions = descriptions.AsReadOnly();
        }

        private static RegisterGroup GetGroupForRegister(List<RegisterGroup> groups, string name, RegisterNameMap nameMap)
        {
            string groupName = nameMap.GetGroupName(name);
            RegisterGroup group = groups.FirstOrDefault((g) => { return g.Name == groupName; });
            if (group == null)
            {
                group = new RegisterGroup(groupName);
                groups.Add(group);
            }
            return group;
        }

        public Dictionary<RegisterGroup, List<Register>> GetRegisters(int threadId, uint level)
        {
            string[] registerValues = null;
            _process.WorkerThread.RunOperation(async () =>
            {
                if (_groups == null || _descriptions == null)
                {
                    Initialize();
                }

                registerValues = await _process.MICommandFactory.DataListRegisterValues(threadId);
            });

            var registerDictionary = new Dictionary<RegisterGroup, List<Register>>();
            foreach (var group in _groups)
            {
                var groupRegisters = new List<Register>();
                foreach (var description in _descriptions)
                {
                    if (description.Group == group)
                    {
                        string content = registerValues[description.Index];
                        groupRegisters.Add(new Register(content, description));
                    }
                }
                registerDictionary.Add(group, groupRegisters);
            }

            return registerDictionary;
        }

        private readonly DebuggedProcess _process;
        private ReadOnlyCollection<RegisterDescription> _descriptions;
        private ReadOnlyCollection<RegisterGroup> _groups;
    }
}
