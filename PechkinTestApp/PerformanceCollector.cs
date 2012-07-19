using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace Html2PdfTestApp
{
    public class TimedAction
    {
        public string Name { get; set; }
        public TimeSpan Duration { get; set; }

        public TimedAction(string name, TimeSpan duration)
        {
            Name = name;
            Duration = duration;
        }
    }

    public class PerformanceCollector
    {
        private string _name = "Perfomance statistics";
        private DateTime _lastMilestone = DateTime.Now;
        private List<TimedAction> _actions = new List<TimedAction>();

        public PerformanceCollector() { }
        public PerformanceCollector(string name)
        {
            _name = name;
        }

        public void FinishAction(string actionName)
        {
            DateTime cur = DateTime.Now;

            _actions.Add(new TimedAction(actionName, cur-_lastMilestone));

            _lastMilestone = cur;
        }

        public TimedAction[] GetListOfDurations()
        {
            return _actions.ToArray();
        }

        public override string ToString()
        {
            StringBuilder ret = new StringBuilder();
            TimeSpan whole = TimeSpan.Zero;

            ret.Append(_name).Append(":\n\n");

            foreach (TimedAction action in _actions)
            {
                ret.Append(action.Name).Append(": ").Append(action.Duration.TotalMilliseconds).Append("ms (")
                    .Append(action.Duration.Ticks).Append("t)\n");

                whole += action.Duration;
            }

            ret.Append("\nTotal: ").Append(whole.TotalMilliseconds).Append("ms (")
                    .Append(whole.Ticks).Append("t)");

            return ret.ToString();
        }

        public void ShowInMessageBox(IWin32Window parent)
        {
            if (parent != null)
            {
                MessageBox.Show(parent, ToString(), _name);
            }
            else
            {
                MessageBox.Show(ToString(), _name);
            }
        }
    }
}
