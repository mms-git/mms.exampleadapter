using MMS.MachineAdapter;
using System;
using System.Collections.Generic;
using System.Text;

namespace MMSExampleAdapter
{
    public class MMSHelloWorldPlugin : IMachineAdapterPluginV1
    {
        public IMachineAdapterV1 StartPlugin()
        {
            try
            {
                return new ExampleAdapter();
            }
            catch (Exception exp)
            {
                Console.WriteLine("Error while plugin initialisation: " + exp);
            }

            return null;
        }

        public void StopPlugin()
        {
        }
    }
}
