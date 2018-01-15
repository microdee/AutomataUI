#region usings
using System;
using System.ComponentModel.Composition;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;
using System.Linq;
using Automata.Data;
using VVVV.Nodes;

using VVVV.Core.Logging;
#endregion usings

namespace VVVV.Nodes
{
    #region PluginInfo
    [PluginInfo(Name = "GetState",
                Category = "AutomataUI Animation",
                Version = "TimeBased",
                Help = "get a state information from AutomataUI",
                Tags = "",
                AutoEvaluate = true)]
    #endregion PluginInfo
    public class GetState : IPluginEvaluate, IPartImportsSatisfiedNotification
    {
        #region fields & pins
        protected IDiffSpread<EnumEntry> EnumState;

        [Input("AutomataUI")]
        public Pin<AutomataUITimeBased> AutomataUI;

        [Output("ElapsedStateTime")]
        public ISpread<double> ElapsedStateTime;
        [Output("ElapsedStateFramed")]
        public ISpread<int> ElapsedStateFrames;

        [Output("FadeInOut")]
        public ISpread<double> FadeInOut;

        [Output("Active State")]
        public ISpread<string> StateActive;

        [Import()]
        public ILogger FLogger;
        [Import()]
        public IIOFactory FIOFactory;
        [Import()]
        IPluginHost FHost;

        string EnumName;

        private bool invalidate = true;

        #endregion fields & pins

        public void OnImportsSatisfied()
        {
            AutomataUI.Connected += Input_Connected;
            AutomataUI.Disconnected += Input_Disconnected;

            FHost.GetNodePath(true, out EnumName); //get unique node path
            EnumName += "AutomataUI"; // add unique name to path
            InputAttribute attr = new InputAttribute("State"); //name of pin
            attr.EnumName = EnumName;
            attr.DefaultEnumEntry = "Init"; //default state
            EnumState = FIOFactory.CreateDiffSpread<EnumEntry>(attr);
        }

        private void Input_Disconnected(object sender, PinConnectionEventArgs args)
        {
            FLogger.Log(LogType.Debug, "DisConnected");
            invalidate = true;
        }

        private void Input_Connected(object sender, PinConnectionEventArgs args)
        {

            invalidate = true;
            FLogger.Log(LogType.Debug, "connected");
        }


        //called when data for any output pin is requested
        public void Evaluate(int SpreadMax)
        {
            //ElapsedStateTime.SliceCount = SpreadMax;

            if (AutomataUI.IsConnected)
            {
                if (invalidate || AutomataUI[0].StatesChanged)
                {
                    EnumManager.UpdateEnum(EnumName, AutomataUI[0].stateList[0].Name, AutomataUI[0].stateList.Select(x => x.Name).ToArray());
                    invalidate = false;
                }

                StateActive.SliceCount = EnumState.SliceCount;
                FadeInOut.SliceCount = ElapsedStateTime.SliceCount = ElapsedStateFrames.SliceCount = StateActive.SliceCount;

                for (int j = 0; j < EnumState.SliceCount; j++)
                {
                    var srcState = AutomataUI[0].stateList.First(ss => ss.Name == EnumState[j].Name);
                    FadeInOut[j] = srcState.FadeProgress;

                    var isElapsedZero = srcState.FadingState == FadingState.Inactive ||
                                        srcState.FadingState == FadingState.FadeIn;

                    ElapsedStateTime[j] = isElapsedZero ? 0 : srcState.ElapsedTime;
                    ElapsedStateFrames[j] = isElapsedZero ? 0 : srcState.ElapsedFrames;
                    StateActive[j] = srcState.FadingState.ToString();
                }
            }
        }
    }
}
