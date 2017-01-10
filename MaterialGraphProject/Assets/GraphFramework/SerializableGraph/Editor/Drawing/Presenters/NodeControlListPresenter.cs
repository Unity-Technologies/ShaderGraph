using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using UnityEditor.Graphing.Drawing;
using UnityEngine;
using Random = System.Random;

namespace UnityEditor.Graphing.Drawing
{
    public sealed class NodeControlListPresenter : AbstractNodeControlListPresenter
    {
        [SerializeField] private List<AbstractNodeControlPresenter> m_Presenters;

        private Random m_Random;

        public override IEnumerable<ScriptableObject> presenters
        {
            get { return m_Presenters.Cast<ScriptableObject>(); }
        }

        private NodeControlListPresenter()
        {
        }

        public void Initialize()
        {
            m_Random = new Random();
            m_Presenters = m_Presenters ?? new List<AbstractNodeControlPresenter>();
            m_Presenters.Add(CreateInstance<NodeControlPresenter>());
            var button = CreateInstance<ButtonNodeControlPresenter>();
            button.Initialize(() =>
            {
                var control = CreateInstance<NodeControlPresenter>();
                control.Initialize(new Vector3(m_Random.Next(50), m_Random.Next(50), m_Random.Next(50)));
                m_Presenters.Insert(m_Random.Next(m_Presenters.Count - 1), control);
            });
            m_Presenters.Add(button);
        }
    }
}
