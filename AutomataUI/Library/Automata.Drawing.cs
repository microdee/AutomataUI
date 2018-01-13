using System;
using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using System.Collections.Generic;
using VVVV.Utils.VMath;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using VVVV.Nodes;
using Automata.Data;
using System.Diagnostics;
using System.Globalization;
using VVVV.Core.Logging;

namespace Automata.Drawing
{

    public class PaintAutomataClass
    {
        #region variables
        public Point StagePos;

        public int StateSize = 70;
        public List<Rectangle> Spreadbuttons = new List<Rectangle>();

        Pen RedPen = new Pen(Color.Red, 1.5f);
        Pen AzurePen = new Pen(Color.Aqua, 1.5f);
        Pen OrangePen = new Pen(Color.DarkOrange, 2.0f);
        Pen GrayPen = new Pen(Color.DarkGray, 1.0f);
        public Pen greenPen = new Pen(Color.FromArgb(255, 0, 255, 0), 10);

        SolidBrush InitBrush = new SolidBrush(Color.FromArgb(0, 255, 234));
        SolidBrush transBackColor = new SolidBrush(Color.FromArgb(20, 20, 20));
        SolidBrush StateBrush = new SolidBrush(Color.FromArgb(50, 50, 50));
        SolidBrush whiteBrush = new SolidBrush(Color.FromArgb(190, 190, 190));
        SolidBrush OrangeBrush = new SolidBrush(Color.Orange);


        Color MyColorDarkOrange = Color.DarkOrange;
        Color MyDarkCyan = Color.DarkCyan;
        Color MyBackgroundColor = Color.FromArgb(20, 20, 20);

        AdjustableArrowCap bigArrow = new AdjustableArrowCap(5, 5);
        AdjustableArrowCap noArrow = new AdjustableArrowCap(0, 0);

        Font myfont = new Font("Serif", 8, FontStyle.Regular);

        StringFormat format = new StringFormat();

        public float dpi = 1;

        public List<GraphicsPath> transitionPaths = new List<GraphicsPath>(); //add transitions paths for bezier curves

        public BezierEditData bezierEdit = new BezierEditData();
        
        
        #endregion

        // Methoden //
        private void PaintTransitions(object sender, PaintEventArgs e)
        {
            var fw = sender as AutomataUITimeBased; // connect to encapsulating class GUIAutomataUINode aka main.cs

            var winkel = 0.0;

            greenPen.Alignment = PenAlignment.Center;
            
            transitionPaths.Clear();

            var i = 0;
            #region lines
           
            foreach (var transition in fw.transitionList) // draw lines
            {
                if (transition.IsPingPong)
                {
                    AzurePen.CustomStartCap = bigArrow;
                    OrangePen.CustomStartCap = bigArrow;
                    AzurePen.DashStyle = DashStyle.Dot;
                }
                else
                {
                    AzurePen.CustomStartCap = noArrow;
                    OrangePen.CustomStartCap = noArrow;
                    AzurePen.DashStyle = DashStyle.Solid;
                }

                foreach (var subtransition in fw.transitionList) // check if there is a return transition to draw double connections correctly
                {
                    if (subtransition.startState.ID == transition.endState.ID && subtransition.endState.ID == transition.startState.ID)
                    {
                        winkel = 0.4;
                        break;
                    }
                    else winkel = 0.0;
                }

                // getting start and endpoint for transition lines
                var myEdgePoints = Lines.GetEdgePoints(State.Center(transition.startState.Bounds), State.Center(transition.endState.Bounds), 40, 40, winkel);

                #region bezierstuff
                //empty path object
                bezierEdit.path = new GraphicsPath();

                //create path with edge positions
                bezierEdit.path.AddBezier(myEdgePoints.A, AddTwoPoints(myEdgePoints.A, transition.startBezierPoint), AddTwoPoints(myEdgePoints.B, transition.endBezierPoint), myEdgePoints.B);

                if (transition == bezierEdit.highlightTransition) e.Graphics.DrawPath(RedPen, bezierEdit.path); // draw red editable bezier
                else if (i == fw.TransitionIndex[fw.ShowSlice[0]] && fw.TransitionTimeOut[fw.ShowSlice[0]] > 0) e.Graphics.DrawPath(OrangePen, bezierEdit.path); // active transition
                else e.Graphics.DrawPath(AzurePen, bezierEdit.path); //draw standard bezier

                transitionPaths.Add(bezierEdit.path); // create list of bezier paths for hitdetection
                i++;
                #endregion                
            }
            #endregion

            #region text
            foreach (var transition in fw.transitionList) // draw text
            {
                foreach (var subtransition in fw.transitionList) // check if there is a return transition to draw double connections correctly
                {
                    if (subtransition.startState.ID == transition.endState.ID && subtransition.endState.ID == transition.startState.ID)
                    {
                        winkel = 0.4;
                        break;
                    }
                    else winkel = 0.0;
                }

                var text = transition.Name + $" ({transition.Seconds:0.000} s)"; // create text
                var stringSize = new SizeF();
                stringSize = e.Graphics.MeasureString(text, myfont, 100);

                var myEdgePoints = Lines.GetEdgePoints(State.Center(transition.startState.Bounds), State.Center(transition.endState.Bounds), 40, 40, winkel);

                //could be optimized and only done once
                var center = CalculateBezierCenter(0.5, myEdgePoints.A,  AddTwoPoints(myEdgePoints.A, transition.startBezierPoint), AddTwoPoints(myEdgePoints.B, transition.endBezierPoint),myEdgePoints.B);


                var Bounds = new Rectangle(
                center,
                new Size(Convert.ToInt32(stringSize.Width + 1),
                Convert.ToInt32(stringSize.Height + 1)));
                Bounds.X = Bounds.X - (Bounds.Size.Width / 2);
                Bounds.Y = Bounds.Y - (Bounds.Size.Height / 2);

                e.Graphics.FillRectangle(transBackColor, Bounds);
                e.Graphics.DrawString(text, myfont, whiteBrush, Bounds, format); //text

                transition.Bounds = Bounds; //set textbounds to transition
            }
            #endregion

        }

        private void PaintBezierHandles(object sender, PaintEventArgs e, Transition transition)
        {

            var fw = sender as AutomataUITimeBased; // connect to encapsulating class GUIAutomataUINode aka main.cs
            var angle = 0.0;
            foreach (var subtransition in fw.transitionList) // check if there is a return transition to draw double connections correctly
            {
                if (subtransition.startState.ID == transition.endState.ID && subtransition.endState.ID == transition.startState.ID)
                {
                    angle = 0.4;
                    break;
                }
                else angle = 0.0;
            }


            var myEdgePoints = Lines.GetEdgePoints(State.Center(transition.startState.Bounds), State.Center(transition.endState.Bounds), 40, 40, angle); //draw line from state to state
         
            bezierEdit.bezierStart = new Rectangle(new Point(myEdgePoints.A.X - 5 + transition.startBezierPoint.X, myEdgePoints.A.Y - 5 + transition.startBezierPoint.Y), new Size(10, 10));
            bezierEdit.bezierEnd = new Rectangle(new Point(myEdgePoints.B.X - 5 + transition.endBezierPoint.X, myEdgePoints.B.Y - 5 + transition.endBezierPoint.Y), new Size(10, 10));


            e.Graphics.DrawLine(GrayPen, myEdgePoints.A,AddTwoPoints(bezierEdit.bezierStart.Location,new Point(5,5)));
            e.Graphics.DrawLine(GrayPen, myEdgePoints.B, AddTwoPoints(bezierEdit.bezierEnd.Location, new Point(5, 5)));

            e.Graphics.FillEllipse(OrangeBrush, bezierEdit.bezierStart);
            e.Graphics.FillEllipse(OrangeBrush, bezierEdit.bezierEnd);

        }

        private void PaintEditTransition(object sender, PaintEventArgs e)
        {
            var fw = sender as AutomataUITimeBased; // connect to encapsulating class GUIAutomataUINode aka main.cs

            if (fw.startConnectionState != null)
            {
                var myEdgePoints = new Lines.EdgePoints();

                if (fw.targetConnectionState != null) // is transition between state and state
                {
                    myEdgePoints = Lines.GetEdgePoints(State.Center(fw.startConnectionState.Bounds), State.Center(fw.targetConnectionState.Bounds), 40, 40, 0.0); //draw line from state to state
                }
                else // is transition between state and mouse
                {
                    myEdgePoints = Lines.GetEdgePoints(State.Center(fw.startConnectionState.Bounds), new Point(fw.x, fw.y), 40, 10, 0.0); // get edge points for mouse
                }
                e.Graphics.DrawLine(RedPen, myEdgePoints.A, myEdgePoints.B); //drawline
            }
        }

        private void PaintStateHighlight(object sender, PaintEventArgs e, int index, Pen penColor)
        {
            var fw = sender as AutomataUITimeBased; // connect to encapsulating class GUIAutomataUINode aka main.cs
            if (fw.stateList.Count > 1)
            {
                var item = fw.stateList.ElementAt(index);
                e.Graphics.DrawEllipse(
                    penColor,
                    item.Bounds.X,
                    item.Bounds.Y,
                    StateSize, StateSize); // active state ring
            }
        }

        private void PaintStates(object sender, PaintEventArgs e)
        {
            var fw = sender as AutomataUITimeBased; // connect to encapsulating class GUIAutomataUINode aka main.cs

            if (fw.stateList.Count != 0)
            {
                foreach (var state in fw.stateList) // Loop through List with foreach.
                {
                    var stateName = state.Name;
                    if (state.Seconds > 0.0001) stateName += $" ({state.Seconds:0.000} s)";
                    if (state.Frames > 0) stateName += $" ({state.Frames} f)";


                    if (state.ID == "Init")
                    {
                        e.Graphics.FillEllipse(InitBrush, state.Bounds); //circle
                        e.Graphics.DrawString(stateName, myfont, transBackColor, state.Bounds, format); //text
                    }
                    else
                    {
                        e.Graphics.FillEllipse(StateBrush, state.Bounds); //circle
                        e.Graphics.DrawString(stateName, myfont, whiteBrush, state.Bounds, format); //text
                    }
                }
            }
        }

        private void PaintSpreadButtons(object sender, PaintEventArgs e)
        {
            var fw = sender as AutomataUITimeBased;
            var stringFormat = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };
            int size = Convert.ToInt16(20 * dpi);
            int posx = Convert.ToInt16(23 * dpi);
            int posy = Convert.ToInt16(30 * dpi);

            if (fw.ActiveStateIndex.SliceCount > 1)
            {
                Spreadbuttons.Clear();
                for (var i = 0; i < fw.ActiveStateIndex.SliceCount; i++)
                {
                    Spreadbuttons.Add(new Rectangle(new Point(i * posx + 10, posy), new Size(size, size)));
                    if (i == fw.ShowSlice[0]) e.Graphics.FillRectangle(OrangeBrush, Spreadbuttons.Last());
                    else e.Graphics.FillRectangle(InitBrush, Spreadbuttons.Last());
                    e.Graphics.DrawString(Convert.ToString(i), myfont, StateBrush, Spreadbuttons.Last(), stringFormat); //text
                }
            }
        }

        public void JoregMode(object sender, bool JMode)
        {
            var fw = sender as AutomataUITimeBased; // connect to encapsulating class GUIAutomataUINode aka main.cs
            if (JMode)
            {
                MyBackgroundColor = Color.FromArgb(230, 230, 230);
                transBackColor = new SolidBrush(MyBackgroundColor);
                whiteBrush = new SolidBrush(Color.FromArgb(0, 0, 0));
                StateBrush = new SolidBrush(Color.FromArgb(205, 205, 205));
                MyColorDarkOrange = Color.FromArgb(154, 154, 154);
                InitBrush = new SolidBrush(Color.FromArgb(102, 102, 102));

                RedPen = new Pen(Color.FromArgb(102, 102, 102), 1.5f);
                AzurePen = new Pen(Color.FromArgb(154, 154, 154), 1.5f);
                OrangePen = new Pen(Color.FromArgb(102, 102, 102), 2.0f);

                RedPen.CustomEndCap = bigArrow;
                AzurePen.CustomEndCap = bigArrow;
                OrangePen.CustomEndCap = bigArrow;

                MyDarkCyan = Color.FromArgb(50, 50, 50);
                fw.Invalidate(); //redraw
            }
            else
            {
                MyBackgroundColor = Color.FromArgb(20, 20, 20);
                transBackColor = new SolidBrush(MyBackgroundColor);
                whiteBrush = new SolidBrush(Color.FromArgb(190, 190, 190));
                StateBrush = new SolidBrush(Color.FromArgb(50, 50, 50));
                MyColorDarkOrange = Color.DarkOrange;
                InitBrush = new SolidBrush(Color.FromArgb(0, 255, 234));

                RedPen = new Pen(Color.Red, 1.5f);
                AzurePen = new Pen(Color.Aqua, 1.5f);
                OrangePen = new Pen(Color.DarkOrange, 2.0f);


                RedPen.CustomEndCap = bigArrow;
                AzurePen.CustomEndCap = bigArrow;
                OrangePen.CustomEndCap = bigArrow;

                MyDarkCyan = Color.DarkCyan;
                fw.Invalidate();
            }
        }

        public void PaintAutomata(object sender, PaintEventArgs e)
        {
            var fw = sender as AutomataUITimeBased; // connect to encapsulating class GUIAutomataUINode aka main.cs

            dpi = e.Graphics.DpiX / 96; // get scaling of windows
            int fontsize = Convert.ToInt16(8 / dpi);
            myfont = new Font("Serif", fontsize, FontStyle.Regular);

            try
            {

                if (fw.stateList.Count > 0)
                {
                    e.Graphics.TranslateTransform(StagePos.X, StagePos.Y); //Move stage
                    e.Graphics.ScaleTransform(dpi, dpi); //dpi scaling 
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias; // shapeman
                    e.Graphics.Clear(MyBackgroundColor);
                    e.Graphics.FillEllipse(StateBrush, fw.x - 10, fw.y - 10, 20, 20); // draw mouse

                    PaintTransitions(sender, e); //draw transitions

                    PaintEditTransition(sender, e); //draw add connection

                    if (fw.stateList.Count > 1)
                    {
                        PaintStateHighlight(sender, e, fw.TargetStateIndex[fw.ShowSlice[0]], new Pen(MyDarkCyan, 10.0f)); //draw target state highlight
                        PaintStateHighlight(sender, e, fw.ActiveStateIndex[fw.ShowSlice[0]], new Pen(MyColorDarkOrange, 10.0f)); //draw active state highlight 
                    }

                    PaintStates(sender, e); //draw states
      
                    if (bezierEdit.HighlightTransitionIndex != null) PaintBezierHandles(sender, e, bezierEdit.highlightTransition); //draw bezierhandles

                    e.Graphics.ScaleTransform(1 / dpi, 1 / dpi); //invert dpi scaling 
                    e.Graphics.TranslateTransform(0 - StagePos.X, 0 - StagePos.Y); //invert move stage
                    myfont = new Font("Serif", 8, FontStyle.Regular);
                    e.Graphics.DrawString(fw.licenseOwner, myfont, whiteBrush, new Rectangle(new Point(10, 10), new Size(400, 100))); //text

                    PaintSpreadButtons(sender, e); //Paint Buttons to select which spread of Automata u want
                }
            }
            catch { }
        }

        public void InitAutomataDrawing()
        {
            //setup stuff
            format.LineAlignment = StringAlignment.Center; //center state text
            format.Alignment = StringAlignment.Center; //center state text

            //pfeilspitze
            RedPen.CustomEndCap = bigArrow;
            AzurePen.CustomEndCap = bigArrow;
            OrangePen.CustomEndCap = bigArrow;
        }

        public static Point CalculateBezierCenter(double t, Point p1, Point p2, Point p3, Point p4)
        {
            var p = new Point();
            var tPower3 = t * t * t;
            var tPower2 = t * t;
            var oneMinusT = 1 - t;
            var oneMinusTPower3 = oneMinusT * oneMinusT * oneMinusT;
            var oneMinusTPower2 = oneMinusT * oneMinusT;
            p.X = Convert.ToInt32(oneMinusTPower3 * p1.X + (3 * oneMinusTPower2 * t * p2.X) + (3 * oneMinusT * tPower2 * p3.X) + tPower3 * p4.X);
            p.Y = Convert.ToInt32(oneMinusTPower3 * p1.Y + (3 * oneMinusTPower2 * t * p2.Y) + (3 * oneMinusT * tPower2 * p3.Y) + tPower3 * p4.Y);
            return p;
        }

        public static Point AddTwoPoints(Point p1, Point p2)
        {
            var sum = new Point
            {
                X = p1.X + p2.X,
                Y = p1.Y + p2.Y
            };
            return sum;
        }

        public class Dialogs
        {
            public static void NumericUpDownKeyPress(object sender, KeyPressEventArgs e)
            {
                if (e.KeyChar.Equals('.') || e.KeyChar.Equals(','))
                {
                    e.KeyChar = ((System.Globalization.CultureInfo)System.Globalization.CultureInfo.CurrentCulture).NumberFormat.NumberDecimalSeparator.ToCharArray()[0];
                }
            }

            public static DialogResult ShowInputDialog(ref string input, ref int frames, ref double seconds, string DialogName, float dpi)
            {
                var size = new System.Drawing.Size(200, 130);

                var inputBox = new Form
                {
                    StartPosition = FormStartPosition.CenterScreen,
                    FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow,
                    ClientSize = size,
                    Text = DialogName
                };



                var textBox = new TextBox
                {
                    Size = new System.Drawing.Size(size.Width - 10, 23),
                    Location = new System.Drawing.Point(5, 5),
                    Text = input
                };
                inputBox.Controls.Add(textBox);

                var timeUpDown = new System.Windows.Forms.NumericUpDown
                {
                    Name = "LockedF",
                    Location = new System.Drawing.Point(5, 39),
                    Size = new System.Drawing.Size(80, 20),
                    TextAlign = System.Windows.Forms.HorizontalAlignment.Right,
                    Maximum = decimal.MaxValue,
                    Value = frames,
                    TabIndex = 0,
                };
                timeUpDown.KeyPress += NumericUpDownKeyPress;
                inputBox.Controls.Add(timeUpDown);

                var framesLabel = new System.Windows.Forms.Label
                {
                    Location = new System.Drawing.Point(88, 42),
                    Size = new System.Drawing.Size(100, 23),
                    Text = "Locked (frames)"
                };
                inputBox.Controls.Add(framesLabel);

                var lockSecUpDown = new System.Windows.Forms.NumericUpDown
                {
                    Name = "LockedS",
                    Location = new System.Drawing.Point(5, 69),
                    Size = new System.Drawing.Size(80, 20),
                    TextAlign = System.Windows.Forms.HorizontalAlignment.Right,
                    Maximum = decimal.MaxValue,
                    DecimalPlaces = 3,
                    Increment = (decimal)0.1,
                    Value = (decimal)seconds,
                    TabIndex = 0,
                };
                lockSecUpDown.KeyPress += NumericUpDownKeyPress;
                inputBox.Controls.Add(lockSecUpDown);

                var lockSecLabel = new System.Windows.Forms.Label
                {
                    Location = new System.Drawing.Point(88, 72),
                    Size = new System.Drawing.Size(100, 23),
                    Text = "Locked (sec)"
                };
                inputBox.Controls.Add(lockSecLabel);

                var okButton = new Button
                {
                    DialogResult = System.Windows.Forms.DialogResult.OK,
                    Name = "okButton",
                    Size = new System.Drawing.Size(90, 23),
                    Text = "&OK",
                    Location = new System.Drawing.Point(5, 100)
                };
                inputBox.Controls.Add(okButton);

                var cancelButton = new Button
                {
                    DialogResult = System.Windows.Forms.DialogResult.Cancel,
                    Name = "cancelButton",
                    Size = new System.Drawing.Size(90, 23),
                    Text = "&Cancel",
                    Location = new System.Drawing.Point(size.Width - 95, 100)
                };
                inputBox.Controls.Add(cancelButton);

                inputBox.AcceptButton = okButton;
                inputBox.CancelButton = cancelButton;

                inputBox.Scale(dpi);
                //inputBox.Size = new Size(1.0f);

                var result = inputBox.ShowDialog();
                if (textBox.Text.Length > 0) input = textBox.Text;
                else input = "empty";
                frames = Convert.ToInt16(timeUpDown.Value);
                seconds = Convert.ToDouble(lockSecUpDown.Value);
                return result;
            }

            public static DialogResult ShowTransitionDialog(ref string input, ref double seconds, ref bool pingpong, string DialogName, float dpi)
            {
                var size = new System.Drawing.Size(200, 130);

                var inputBox = new Form
                {
                    StartPosition = FormStartPosition.CenterScreen,

                    FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow,
                    ClientSize = size,
                    Text = DialogName
                };

                var textBox = new TextBox
                {
                    Size = new System.Drawing.Size(size.Width - 10, 23),
                    Location = new System.Drawing.Point(5, 5),
                    Text = input
                };
                inputBox.Controls.Add(textBox);

                var timeUpDown = new System.Windows.Forms.NumericUpDown
                {
                    Name = "TimeS",
                    Location = new System.Drawing.Point(5, 39),
                    Size = new System.Drawing.Size(80, 20),
                    TextAlign = System.Windows.Forms.HorizontalAlignment.Right,
                    Maximum = decimal.MaxValue,
                    
                    DecimalPlaces = 3,
                    Increment = (decimal)0.1,
                    TabIndex = 0,
                    Value = (decimal)seconds
                };
                timeUpDown.KeyPress += NumericUpDownKeyPress;
                inputBox.Controls.Add(timeUpDown);

                var durLabel = new System.Windows.Forms.Label
                {
                    Location = new System.Drawing.Point(88, 42),
                    Size = new System.Drawing.Size(100, 23),
                    Text = "Duration (sec)"
                };
                inputBox.Controls.Add(durLabel);

                var isPingPong = new System.Windows.Forms.CheckBox
                {
                    Location = new System.Drawing.Point(70, 70),
                    Text = "PingPong",
                    Checked = pingpong // getting the bool from the transition object
                };
                inputBox.Controls.Add(isPingPong);



                var okButton = new Button
                {
                    DialogResult = System.Windows.Forms.DialogResult.OK,
                    Name = "okButton",
                    Size = new System.Drawing.Size(90, 23),
                    Text = "&OK",
                    Location = new System.Drawing.Point(5, 99)
                };
                inputBox.Controls.Add(okButton);

                var cancelButton = new Button
                {
                    DialogResult = System.Windows.Forms.DialogResult.Cancel,
                    Name = "cancelButton",
                    Size = new System.Drawing.Size(90, 23),
                    Text = "&Cancel",
                    Location = new System.Drawing.Point(size.Width - 95, 99)
                };
                inputBox.Controls.Add(cancelButton);

                inputBox.AcceptButton = okButton;
                inputBox.CancelButton = cancelButton;

                inputBox.Scale(dpi);

                var result = inputBox.ShowDialog();
                if (textBox.Text.Length > 0) input = textBox.Text;
                else input = "empty";
                seconds = Convert.ToDouble(timeUpDown.Value);
                pingpong = isPingPong.Checked;
                return result;
            }
        }

    }

    public class BezierEditData
    {
        public GraphicsPath path
        {
            get;
            set;
        }

        public Transition highlightTransition
        {
            get;
            set;
        }

        public int? HighlightTransitionIndex
        {
            get;
            set;
        }

        public Rectangle bezierStart
        {
            get;
            set;
        }

        public Rectangle bezierEnd
        {
            get;
            set;
        }

        public Point originStart
        {
            get;
            set;
        }

        public Point originEnd
        {
            get;
            set;
        }

    }

    public class Lines
    {
        //datatype for edge to edge connection
        public class EdgePoints
        {
            public Point A
            {
                get;
                set;
            }

            public Point B
            {
                get;
                set;
            }

            public Point Center
            {
                get;
                set;
            }
        }


        //calculate edgepoints from state position and radius
        public static EdgePoints GetEdgePoints(Point A, Point B, int Radius, int Radius2, double winkel)
        {

            var PointA = new Vector3D(A.X, A.Y, 0);
            var PointB = new Vector3D(B.X, B.Y, 0);

            Vector3D TempA;
            Vector3D TempB;
            Vector3D TempC;

            var tempVector = new Vector3D(VMath.PolarVVVV(PointA - PointB)); //get Polar Values

            if (tempVector.y > 0) // depending which quadrant of rotation
            {
                TempA = VMath.CartesianVVVV(tempVector.x + winkel, tempVector.y, 0 - Radius) + PointA; //minus Radius from Length > into Cartesian
                TempB = VMath.CartesianVVVV(tempVector.x - winkel, tempVector.y, Radius2) + PointB; //Radius is Length > into Cartesian
            }
            else
            {
                TempA = VMath.CartesianVVVV(tempVector.x - winkel, tempVector.y, 0 - Radius) + PointA; //minus Radius from Length > into Cartesian
                TempB = VMath.CartesianVVVV(tempVector.x + winkel, tempVector.y, Radius2) + PointB; //Radius is Length > into Cartesian
            }

            TempC = VMath.CartesianVVVV(VMath.PolarVVVV(TempA - TempB).x, VMath.PolarVVVV(TempA - TempB).y, 0 - VMath.PolarVVVV(TempA - TempB).z / 2.75) + TempA; // calculate center

            var myEdgeCoords = new EdgePoints
            {
                A = new Point(Convert.ToInt16(TempA.x), Convert.ToInt16(TempA.y)), // create Point from Vector
                B = new Point(Convert.ToInt16(TempB.x), Convert.ToInt16(TempB.y)), // create Point from Vector
                Center = new Point(Convert.ToInt16(TempC.x), Convert.ToInt16(TempC.y))
            }; // edgepoint definition

            return myEdgeCoords;
        }
    }

    public class PanZoom
    {
        public class PanZoomValues
        {
            public Point Pan
            {
                get;
                set;
            }

            public Point PanPrevious
            {
                get;
                set;
            }

            public float Zoom
            {
                get;
                set;
            }
        }

        public PanZoomValues MovePan()
        {
            var tempPanZoom = new PanZoomValues();
            return tempPanZoom;
        }
    }

}