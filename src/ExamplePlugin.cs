using MMS.MachineAdapter;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Timers;

namespace MMSExampleAdapter
{
    public class ExampleAdapter : IMachineAdapterV1
    {
        public Guid Id { get; set; }

        private string _Name = "";
        public string Name { get { return _Name; } set { _Name = value; MachineInformationChanged?.Invoke(); } }

        private string _Description = "";
        public string Description { get { return _Description; } set { _Description = value; MachineInformationChanged?.Invoke(); } }

        private string _Homepage = "";
        public string Homepage { get { return _Homepage; } set { _Homepage = value; MachineInformationChanged?.Invoke(); } }

        private byte[] _Icon = new byte[0];
        public byte[] Icon { get { return _Icon; } set { _Icon = value; MachineInformationChanged?.Invoke(); } }

        public MachineOrderData OrderData { get; set; }

        public MachinePositionData Position { get; set; }

        public MachineStateData StateData { get; set; }

        public Dictionary<string, string> Parameters { get; set; }

        public Dictionary<string, string> Tags { get; set; }

        public event MachineInformationChangedHandler MachineInformationChanged;
        public event MachineOrderDataChangedHandler MachineOrderDataChanged;
        public event MachineStateDataChangedHandler MachineStateDataChanged;
        public event MachinePositionDataChangedHandler MachinePositionDataChanged;
        public event MachineParametersChangedHandler MachineParametersChanged;

        private Timer _StartTimer;
        private Timer _FinishedTimer;

        public ExampleAdapter()
        {
            Name = "HelloWorldAdapter";
            Description = "Example machine adapter for demo purpose";
            Homepage = "http:\\www.eckelmann.de";

            //Initialises Icon from embeded ressource
            Icon = ImageConvert.ImageArrayFromResource(Assembly.GetAssembly(GetType()), "MMS-Logo.png");

            //Initalises the orderdata with default informations (no order)
            OrderData = new MachineOrderData();
            OrderData.ProgramName = "";
            OrderData.OrderName = "";
            OrderData.OrderId = 0;
            OrderData.OrderState = MachineOrderStates.Unknown;

            //Sets the machine geo-position
            Position = new MachinePositionData();
            Position.GeoPosition = "50°03'35.84\" N 8°17'06.36\" O";

            StateData = new MachineStateData();
            Parameters = new Dictionary<string, string>();
            Tags = new Dictionary<string, string>();

            //Simulates a preparation time till the start of an order
            _StartTimer = new Timer(2000.0);
            _StartTimer.Elapsed += StartTimer_Elapsed;

            //Simulates the processing time of an order till it's finished.
            _FinishedTimer = new Timer(10000.0);
            _FinishedTimer.Elapsed += FinishedTimer_Elapsed;


            //Sets the inital state to idle
            ChangeState(MachineStates.Idle, MachineStates.Operating, Int32.MaxValue);
        }

        //Cancelation of a running order
        public bool CancelOrder(long orderId)
        {
            _StartTimer.Stop();
            _FinishedTimer.Stop();

            ChangeState(MachineStates.Idle, MachineStates.Operating, Int32.MaxValue);

            OrderData.OrderState = MachineOrderStates.Canceled;
            MachineOrderDataChanged?.Invoke(OrderData);

            return true;
        }

        //Starts the processing of the transfered order/programFile
        public bool TransferOrder(MachineOrderData orderData, byte[] programFile)
        {
            if (StateData.CurrentState == MachineStates.Idle)
            {
                OrderData = orderData;
                OrderData.OrderState = MachineOrderStates.Transfered;
                MachineOrderDataChanged?.Invoke(OrderData);

                _StartTimer.Start();

                return true;
            }

            return false;
        }

        //Preparation finsihed
        private void StartTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            _StartTimer.Stop();
            _FinishedTimer.Start();

            //Preparation is completed now start the processing 
            ChangeState(MachineStates.Operating, MachineStates.Idle, 10);

            //Machine starts the processing, set OrderState to started
            OrderData.OrderState = MachineOrderStates.Started;
            MachineOrderDataChanged?.Invoke(OrderData);
        }

        private void FinishedTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            _StartTimer.Stop();
            _FinishedTimer.Stop();

            //Processing is completed machine is going back to idle
            ChangeState(MachineStates.Idle, MachineStates.Operating, Int32.MaxValue);

            //Processing is completed, set order state to finished complete
            OrderData.OrderState = MachineOrderStates.FinishedComplete;
            MachineOrderDataChanged?.Invoke(OrderData);
        }

        private void ChangeState(MachineStates newState, MachineStates nextState= MachineStates.Idle, int predictedNextChangeSeconds = 10)
        {
            //Copy last state
            StateData.PreviousState = StateData.CurrentState;

            //Set new state and changetimestamp (right now)
            StateData.CurrentState = newState;
            StateData.CurrentStateChangedTimestamp = DateTime.Now;

            //Set predicted state which this adapter will be going to and a predicted time when it will change.
            StateData.PredictedState = nextState;
            StateData.PredictedStateChangeTime = DateTime.Now.AddSeconds(predictedNextChangeSeconds);

            //Publishes the state change and the orderId indicates the context of the state change.
            MachineStateDataChanged?.Invoke(StateData, OrderData.OrderId);
        }
    }
}
