// COPYRIGHT 2012, 2013 by the Open Rails project.
// 
// This file is part of Open Rails.
// 
// Open Rails is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Open Rails is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Open Rails.  If not, see <http://www.gnu.org/licenses/>.

// This file is the responsibility of the 3D & Environment Team. 

using System;
using System.Windows.Forms;
using Microsoft.Xna.Framework;
using Orts.Common;
using Orts.Viewer3D.Processes;
using ORTS.Common.Input;

namespace Orts.Viewer3D.Popups
{

    public class HeadlightEditorWindow : Window
    {   
        public Label position_X, position_Y, position_Z, direction_X, direction_Y, direction_Z, distance, Color_R, Color_G, Color_B;
        public Label Bnt_position_Sel, Bnt_direction_Sel, Bnt_distance_Sel, Bnt_Color_Sel;
        public enum EnuSelection 
        {       
           Notselected, position, direction , distance, Color, NumberofStates
        };

        public EnuSelection EnmSelect = EnuSelection.Notselected;
        Color Color_Selected = new Color(0xFF,0x9C,0x01);
        Color Color_Unselected = Color.LightGray;
       
        public HeadlightEditorWindow(WindowManager owner)
            : base(owner, Window.DecorationSize.X + owner.TextFontDefault.Height * 38
                  , Window.DecorationSize.Y + owner.TextFontDefault.Height * 16 + ControlLayout.SeparatorSize * 3, Viewer.Catalog.GetString("Headlight Editor"))
        {
        }

        protected override ControlLayout Layout(ControlLayout layout)
        {
            var vbox = base.Layout(layout).AddLayoutVertical();
            var boxWidth = vbox.RemainingWidth / 8;

            var hbox = vbox.AddLayoutHorizontalLineOfText();
            hbox.Add(Bnt_position_Sel = new Label(boxWidth, Owner.TextFontDefault.Height," Position",LabelAlignment.Left, Color.Azure));
            Bnt_position_Sel.Click += new Action<Control, Point>(Bnt_position_Sel_Click);
            hbox.Add(              new Label(90,  hbox.RemainingHeight," Position X: ",LabelAlignment.Left, Color.Azure));
            hbox.Add(position_X =  new Label(60, hbox.RemainingHeight,"0.0",LabelAlignment.Left, Color.Azure));
            
            hbox.Add(             new Label(90,  hbox.RemainingHeight," Position Y: ",LabelAlignment.Left, Color.Azure));
            hbox.Add(position_Y = new Label(60, hbox.RemainingHeight,"0.0",LabelAlignment.Left, Color.Azure));
            
            hbox.Add(              new Label(90,  hbox.RemainingHeight," Position Z: ",LabelAlignment.Left, Color.Azure));
            hbox.Add(position_Z =  new Label(60, hbox.RemainingHeight,"0.0",LabelAlignment.Left, Color.Azure));
            vbox.AddHorizontalSeparator();

            hbox = vbox.AddLayoutHorizontalLineOfText();
            hbox.Add(Bnt_direction_Sel = new Label(boxWidth, Owner.TextFontDefault.Height," Direction",LabelAlignment.Left, Color.Azure));
            Bnt_direction_Sel.Click += new Action<Control, Point>(Bnt_direction_Sel_Click);
        
            hbox.Add(              new Label(90, hbox.RemainingHeight," Direction X: ",LabelAlignment.Left, Color.Azure));
            hbox.Add(direction_X = new Label(60, hbox.RemainingHeight,"0.0",LabelAlignment.Left, Color.Azure));
        
            hbox.Add(              new Label(90, hbox.RemainingHeight," Direction Y: ",LabelAlignment.Left, Color.Azure));
            hbox.Add(direction_Y = new Label(60, hbox.RemainingHeight,"0.0",LabelAlignment.Left, Color.Azure));
            
            hbox.Add(              new Label(90, hbox.RemainingHeight," Direction Z: ",LabelAlignment.Left, Color.Azure));
            hbox.Add(direction_Z = new Label(60, hbox.RemainingHeight,"0.0",LabelAlignment.Left, Color.Azure));
            vbox.AddHorizontalSeparator();


            hbox = vbox.AddLayoutHorizontalLineOfText();
            hbox.Add(Bnt_distance_Sel = new Label(boxWidth, Owner.TextFontDefault.Height," Distance",LabelAlignment.Left, Color.Azure));
            Bnt_distance_Sel.Click += new Action<Control, Point>(Bnt_distance_Sel_Click);
        
            hbox.Add(           new Label(80, hbox.RemainingHeight," Distance: ",LabelAlignment.Left, Color.Azure));
            hbox.Add(distance = new Label(50 , hbox.RemainingHeight,"0.0",LabelAlignment.Left, Color.Azure));
        
            vbox.AddHorizontalSeparator();
        
            vbox.AddSpace(0, 4);

            Console.Write( "Headlight Window Gui Created");
            ClearLabelColor();

            return vbox;
        }


        public void ClearLabelColor()
        {   
            // Scenery
            Bnt_position_Sel.Color = Color_Unselected;
            Bnt_direction_Sel.Color = Color_Unselected;
            Bnt_distance_Sel.Color = Color_Unselected;
            distance.Color = Color_Unselected;
            position_X.Color = Color_Unselected;
            position_Y.Color    = Color_Unselected;
            position_Z.Color  = Color_Unselected;
            direction_X.Color = Color_Unselected;
            direction_Y.Color = Color_Unselected;
            direction_Z.Color    = Color_Unselected;
        }

        // ------------- Callback section ------------------- 

        void Bnt_position_Sel_Click(Control arg1, Point arg2)
        {
            ClearLabelColor();
            EnmSelect = EnuSelection.position;
            position_X.Color = Color_Selected;
            position_Y.Color = Color_Selected;
            position_Z.Color = Color_Selected;
            Bnt_position_Sel.Color = Color_Selected;
        }

        void Bnt_direction_Sel_Click(Control arg1, Point arg2)
        {
            ClearLabelColor();
            EnmSelect = EnuSelection.direction;
            direction_X.Color = Color_Selected;
            direction_Y.Color = Color_Selected;
            direction_Z.Color = Color_Selected;
            Bnt_direction_Sel.Color = Color_Selected;
        }
        
        void Bnt_distance_Sel_Click(Control arg1, Point arg2)
        {
            ClearLabelColor();
            EnmSelect = EnuSelection.distance;
            distance.Color = Color_Selected;
            Bnt_distance_Sel.Color = Color_Selected;
        }
        

        
    }
}
