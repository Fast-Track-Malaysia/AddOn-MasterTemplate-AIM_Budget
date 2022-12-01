using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FT_ADDON
{
    abstract class STASafe
    {
        Action action;

        /// <summary>
        /// Create a new public function with better name to wrap this function
        /// </summary>
        protected void ExecuteSafeAction()
        {
            if (action.GetInvocationList().Length == 0) throw new Exception("STA Safe action is empty");

            Thread t = new Thread(() =>
            {
                action();
            });
            t.SetApartmentState(ApartmentState.STA);
            t.Start();
            t.Join();
        }

        /// <summary>
        /// Recommended use is to add action(s) during the construction of the derived class
        /// </summary>
        /// <param name="action"></param>
        protected void AddSafeAction(Action action)
        {
            this.action += action;
        }
    }
}
