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
    public class WeatherEditorWindow : Window
    {   
        public Label Time, SunriseTime, SunsetTime, CurrentTime, WeatherType, Season; 
        Label Bnt_ScViewDistance_Sel, Bnt_ScFC_Sunrise_Sel, Bnt_ScFC_Noon_Sel, Bnt_ScFC_Sunset_Sel, Bnt_Wind_Sel, Bnt_Overcast_Sel; 
        public Label ScFCSunrise_R, ScFCSunrise_G, ScFCSunrise_B; 
        public Label ScFCNoon_R, ScFCNoon_G, ScFCNoon_B;
        public Label ScFCSunset_R, ScFCSunset_G, ScFCSunset_B, ScFCCurrent; 
        public Label ScVDSunrise, ScVDNoon, ScVDSunset, ScVDPrecentMix;

        public Label WindSpeed, WindDirSky;
        public Label Overcast1, Overcast2, Overcast3;
        public Label PrecipLiquid, PricipIntPPSPM2, Bnt_Precip_Sel;
        
        public Label PrecipWind1X, PrecipWind1Y, PrecipWind1Z, Bnt_PrecipWind1_Sel;
        public Label PrecipWind2X, PrecipWind2Y, PrecipWind2Z, Bnt_PrecipWind2_Sel;
        public Label PrecipParSize1, PrecipParSize2, Bnt_PrecipParSize_Sel;

        Label Bnt_SkyViewDistance_Sel, Bnt_SkyFC_Sunrise_Sel, Bnt_SkyFC_Noon_Sel, Bnt_SkyFC_Sunset_Sel, Bnt_SkySunSize_Sel;
        public Label SkyFCSunrise_R, SkyFCSunrise_G, SkyFCSunrise_B; 
        public Label SkyFCNoon_R,    SkyFCNoon_G,    SkyFCNoon_B;
        public Label SkyFCSunset_R,  SkyFCSunset_G,  SkyFCSunset_B, SkyFCCurrent;
        public Label SkyVDSunrise, SkyVDNoon, SkyVDSunset, SkyVDPrecentMix;

        public Label SkySunSizeSunrise, SkySunSizeNoon, SkySunSizeSunset ,SkySunSizeCurrent;

        public Label VegDesatMod, VegBrightMod, VegContMod, Bnt_VegMod_Sel;
        public Label TerrDesatMod, TerrBrightMod, TerrContMod, Bnt_TerrMod_Sel;
        public Label Bnt_ReloadWeathertype_Sel, Bnt_SaveWeathertype_Sel;

        public enum EnuSelection 
        {       
                Notselected, ScFC_Sunrise_Sel, ScFC_Noon_Sel, ScFC_Sunset_Sel, ScViewDistance_Sel, SkyViewDistance_Sel
                , SkyFC_Sunrise_Sel, SkyFC_Noon_Sel, SkyFC_Sunset_Sel, SkyVD_Sunrise_Sel, SkyVD_Noon_Sel
                , SkyVD_Sunset_Sel, Wind_Sel, Overcast, SkySunSize_Sel, PrecipWind1_Sel, PrecipWind2_Sel
                , PrecipParSize_Sel,VegMod_Sel, TerrMod_Sel, Precip_Sel ,NumberofStates
        };

        public EnuSelection EnmSelect = EnuSelection.Notselected;
        public Color Color_Selected = new Color(0xFF,0x9C,0x01);
        public Color Color_Unselected = Color.LightGray;
        public Color Color_ReLoaded = Color.GreenYellow;
       
        public WeatherEditorWindow(WindowManager owner)
            : base(owner, Window.DecorationSize.X + owner.TextFontDefault.Height * 36
                  , Window.DecorationSize.Y + owner.TextFontDefault.Height * 32 + ControlLayout.SeparatorSize * 3, Viewer.Catalog.GetString("Weather Editor 3.3"))
        {
        }

        protected override ControlLayout Layout(ControlLayout layout)
        {
            var vbox = base.Layout(layout).AddLayoutVertical();
            var boxWidth = vbox.RemainingWidth / 8;

            var hbox = vbox.AddLayoutHorizontalLineOfText();
            hbox.Add(              new Label(55,  hbox.RemainingHeight," Time: ",LabelAlignment.Left, Color.Azure));
            hbox.Add(Time =        new Label(100, hbox.RemainingHeight,"00:00:",LabelAlignment.Left, Color.Azure));
            
            hbox.Add(              new Label(70,  hbox.RemainingHeight," Sunrise: ",LabelAlignment.Left, Color.Azure));
            hbox.Add(SunriseTime = new Label(100, hbox.RemainingHeight,"00:00:",LabelAlignment.Left, Color.Azure));
            
            hbox.Add(              new Label(70,  hbox.RemainingHeight," Sunset : ",LabelAlignment.Left, Color.Azure));
            hbox.Add(SunsetTime =  new Label(100, hbox.RemainingHeight,"00:00:",LabelAlignment.Left, Color.Azure));
            vbox.AddHorizontalSeparator();

            hbox = vbox.AddLayoutHorizontalLineOfText();
            hbox.Add(              new Label(145, hbox.RemainingHeight," Simulation Time: ",LabelAlignment.Left, Color.Azure));
            hbox.Add(CurrentTime = new Label(100, hbox.RemainingHeight,"----------",LabelAlignment.Left, Color.Azure));
        
            hbox.Add(              new Label(122, hbox.RemainingHeight," Weather Type: ",LabelAlignment.Left, Color.Azure));
            hbox.Add(WeatherType = new Label(100, hbox.RemainingHeight,"----------",LabelAlignment.Left, Color.Azure));
            
            hbox.Add(              new Label(70, hbox.RemainingHeight," Season: ",LabelAlignment.Left, Color.Azure));
            hbox.Add(Season      = new Label(100, hbox.RemainingHeight,"00:00:",LabelAlignment.Left, Color.Azure));
            vbox.AddHorizontalSeparator();

            // Wind speed & Direction   
            hbox = vbox.AddLayoutHorizontalLineOfText();
            hbox.Add(Bnt_Wind_Sel = new Label(boxWidth*3, Owner.TextFontDefault.Height," Wind - Speed & Direction",LabelAlignment.Left));
            Bnt_Wind_Sel.Click += new Action<Control, Point>(Bnt_Wind_Sel_Click);
            hbox.Add(WindSpeed = new Label(boxWidth, hbox.RemainingHeight, "00", LabelAlignment.Center));
            hbox.Add(WindDirSky = new Label(boxWidth, hbox.RemainingHeight, "0", LabelAlignment.Center));
            vbox.AddHorizontalSeparator();

            // View Distance header
            hbox = vbox.AddLayoutHorizontalLineOfText();
            hbox.Add(new Label(boxWidth*3, Owner.TextFontDefault.Height," ",LabelAlignment.Left));
            hbox.Add(new Label(boxWidth, hbox.RemainingHeight, "Sunrise ↑" , LabelAlignment.Center, Color.LightSteelBlue) );
            hbox.Add(new Label(boxWidth, hbox.RemainingHeight, "Noon    ☼", LabelAlignment.Center, Color.LightSteelBlue) );
            hbox.Add(new Label(boxWidth, hbox.RemainingHeight, "Sunset  ↓"  , LabelAlignment.Center, Color.LightSteelBlue) );
            hbox.Add(new Label(boxWidth, hbox.RemainingHeight, "Current"  , LabelAlignment.Center, Color.LightSteelBlue) );

            // SunSize   
            hbox = vbox.AddLayoutHorizontalLineOfText();
            hbox.Add(Bnt_SkySunSize_Sel = new Label(boxWidth*3, Owner.TextFontDefault.Height," Sun Size ☼",LabelAlignment.Left));
            Bnt_SkySunSize_Sel.Click += new Action<Control, Point>(Bnt_SkySunSize_Sel_Click);
            hbox.Add(SkySunSizeSunrise = new Label(boxWidth, hbox.RemainingHeight, "0.0", LabelAlignment.Center));
            hbox.Add(SkySunSizeNoon    = new Label(boxWidth, hbox.RemainingHeight, "0.0", LabelAlignment.Center));
            hbox.Add(SkySunSizeSunset  = new Label(boxWidth, hbox.RemainingHeight, "0.0", LabelAlignment.Center));
            hbox.Add(SkySunSizeCurrent = new Label(boxWidth, hbox.RemainingHeight, "0.0", LabelAlignment.Center));

            // Sky Fog View Distance
            hbox = vbox.AddLayoutHorizontalLineOfText();
            hbox.Add(Bnt_SkyViewDistance_Sel = new Label(boxWidth*3, Owner.TextFontDefault.Height," Sky View Distance",LabelAlignment.Left));
            Bnt_SkyViewDistance_Sel.Click += new Action<Control, Point>(Bnt_SkyViewDistance_sel_Click);
            hbox.Add(SkyVDSunrise = new Label(boxWidth, hbox.RemainingHeight, "0000", LabelAlignment.Center));
            hbox.Add(SkyVDNoon    = new Label(boxWidth, hbox.RemainingHeight, "0000", LabelAlignment.Center));
            hbox.Add(SkyVDSunset  = new Label(boxWidth, hbox.RemainingHeight, "0000", LabelAlignment.Center));
            hbox.Add(SkyVDPrecentMix  = new Label(boxWidth, hbox.RemainingHeight, "0000", LabelAlignment.Center));
            vbox.AddSpace(0, 4); 

            // Scenery Fog View Distance
            hbox = vbox.AddLayoutHorizontalLineOfText();
            hbox.Add(Bnt_ScViewDistance_Sel = new Label(boxWidth*3, Owner.TextFontDefault.Height," Scenery View Distance",LabelAlignment.Left));
            Bnt_ScViewDistance_Sel.Click += new Action<Control, Point>(Bnt_ScViewDistance_sel_Click);
            hbox.Add(ScVDSunrise = new Label(boxWidth, hbox.RemainingHeight, "0000", LabelAlignment.Center));
            hbox.Add(ScVDNoon    = new Label(boxWidth, hbox.RemainingHeight, "0000", LabelAlignment.Center));
            hbox.Add(ScVDSunset  = new Label(boxWidth, hbox.RemainingHeight, "0000", LabelAlignment.Center));
            hbox.Add(ScVDPrecentMix  = new Label(boxWidth, hbox.RemainingHeight, "0000", LabelAlignment.Center));
            vbox.AddSpace(0, 4);

            // Sky
            vbox.AddHorizontalSeparator();
            hbox = vbox.AddLayoutHorizontalLineOfText();
            hbox.Add(new Label(boxWidth * 3, hbox.RemainingHeight, Viewer.Catalog.GetString("  ")));
            hbox.Add(new Label(boxWidth, hbox.RemainingHeight, "Red"  , LabelAlignment.Center, Color.IndianRed) );
            hbox.Add(new Label(boxWidth, hbox.RemainingHeight, "Green", LabelAlignment.Center, Color.LightGreen) );
            hbox.Add(new Label(boxWidth, hbox.RemainingHeight, "Blue" , LabelAlignment.Center, Color.RoyalBlue) );
            hbox.Add(new Label(boxWidth, hbox.RemainingHeight, "Color Mix" , LabelAlignment.Center, Color.LightSteelBlue) );
            vbox.AddSpace(0, 4); 

            hbox = vbox.AddLayoutHorizontalLineOfText();
            hbox.AddSpace(0, 2);
            hbox.Add(Bnt_SkyFC_Sunrise_Sel = new Label(boxWidth*3, Owner.TextFontDefault.Height," Sky Fog Color Sunrise ↑",LabelAlignment.Left));
            Bnt_SkyFC_Sunrise_Sel.Click += new Action<Control, Point>(Bnt_SkyFC_Sunrise_Sel_Click);
            hbox.Add(SkyFCSunrise_R = new Label(boxWidth, hbox.RemainingHeight, "255", LabelAlignment.Center));
            hbox.Add(SkyFCSunrise_G = new Label(boxWidth, hbox.RemainingHeight, "255", LabelAlignment.Center));
            hbox.Add(SkyFCSunrise_B = new Label(boxWidth, hbox.RemainingHeight, "255", LabelAlignment.Center));
            hbox.Add(SkyFCCurrent   = new Label(boxWidth, hbox.RemainingHeight, "████████", LabelAlignment.Center));
            vbox.AddSpace(0, 2);

            hbox = vbox.AddLayoutHorizontalLineOfText();
            hbox.Add(Bnt_SkyFC_Noon_Sel = new Label(boxWidth*3, Owner.TextFontDefault.Height,   " Sky Fog Color Noon    ☼",LabelAlignment.Left));
            Bnt_SkyFC_Noon_Sel.Click += new Action<Control, Point>(Bnt_SkyFC_Noon_Sel_Click);
            hbox.Add(SkyFCNoon_R = new Label(boxWidth, hbox.RemainingHeight, "255", LabelAlignment.Center));
            hbox.Add(SkyFCNoon_G = new Label(boxWidth, hbox.RemainingHeight, "255", LabelAlignment.Center));
            hbox.Add(SkyFCNoon_B = new Label(boxWidth, hbox.RemainingHeight, "255", LabelAlignment.Center));
            vbox.AddSpace(0, 2);        
        
            hbox = vbox.AddLayoutHorizontalLineOfText();
            hbox.Add(Bnt_SkyFC_Sunset_Sel = new Label(boxWidth*3, Owner.TextFontDefault.Height," Sky Fog Color Sunset  ↓",LabelAlignment.Left));
            Bnt_SkyFC_Sunset_Sel.Click += new Action<Control, Point>(Bnt_SkyFC_Sunset_Sel_Click);
            hbox.Add(SkyFCSunset_R = new Label(boxWidth, hbox.RemainingHeight, "255", LabelAlignment.Center));
            hbox.Add(SkyFCSunset_G = new Label(boxWidth, hbox.RemainingHeight, "255", LabelAlignment.Center));
            hbox.Add(SkyFCSunset_B = new Label(boxWidth, hbox.RemainingHeight, "255", LabelAlignment.Center));
            vbox.AddSpace(0, 14);

            // Scenery
            hbox = vbox.AddLayoutHorizontalLineOfText();
            hbox.AddSpace(0, 8);
            hbox.Add(Bnt_ScFC_Sunrise_Sel = new Label(boxWidth*3, Owner.TextFontDefault.Height," Scenery Fog Color Sunrise ↑",LabelAlignment.Left));
            Bnt_ScFC_Sunrise_Sel.Click += new Action<Control, Point>(Bnt_ScFC_Sunrise_Sel_Click);
            hbox.Add(ScFCSunrise_R = new Label(boxWidth, hbox.RemainingHeight, "255", LabelAlignment.Center));
            hbox.Add(ScFCSunrise_G = new Label(boxWidth, hbox.RemainingHeight, "255", LabelAlignment.Center));
            hbox.Add(ScFCSunrise_B = new Label(boxWidth, hbox.RemainingHeight, "255", LabelAlignment.Center));
            hbox.Add(ScFCCurrent   = new Label(boxWidth, hbox.RemainingHeight, "████████", LabelAlignment.Center));
            vbox.AddSpace(0, 2);
                
            hbox = vbox.AddLayoutHorizontalLineOfText();
            hbox.Add(Bnt_ScFC_Noon_Sel = new Label(boxWidth*3, Owner.TextFontDefault.Height,   " Scenery Fog Color Noon    ☼",LabelAlignment.Left));
            Bnt_ScFC_Noon_Sel.Click += new Action<Control, Point>(Bnt_ScFC_Noon_Sel_Click);
            hbox.Add(ScFCNoon_R = new Label(boxWidth, hbox.RemainingHeight, "255", LabelAlignment.Center));
            hbox.Add(ScFCNoon_G = new Label(boxWidth, hbox.RemainingHeight, "255", LabelAlignment.Center));
            hbox.Add(ScFCNoon_B = new Label(boxWidth, hbox.RemainingHeight, "255", LabelAlignment.Center));
            vbox.AddSpace(0, 2);        
        
            hbox = vbox.AddLayoutHorizontalLineOfText();
            hbox.Add(Bnt_ScFC_Sunset_Sel = new Label(boxWidth*3, Owner.TextFontDefault.Height, " Scenery Fog Color Sunset  ↓",LabelAlignment.Left));
            Bnt_ScFC_Sunset_Sel.Click += new Action<Control, Point>(Bnt_ScFC_Sunset_Sel_Click);
            hbox.Add(ScFCSunset_R = new Label(boxWidth, hbox.RemainingHeight, "255", LabelAlignment.Center));
            hbox.Add(ScFCSunset_G = new Label(boxWidth, hbox.RemainingHeight, "255", LabelAlignment.Center));
            hbox.Add(ScFCSunset_B = new Label(boxWidth, hbox.RemainingHeight, "255", LabelAlignment.Center));
            vbox.AddSpace(0, 2); 
            vbox.AddHorizontalSeparator();

            // Overcast header
            hbox = vbox.AddLayoutHorizontalLineOfText();
            hbox.Add(new Label(boxWidth*3, Owner.TextFontDefault.Height," ",LabelAlignment.Left));
            hbox.Add(new Label(boxWidth, hbox.RemainingHeight, "Ꝍ 1"  , LabelAlignment.Center, Color.LightSteelBlue) );
            hbox.Add(new Label(boxWidth, hbox.RemainingHeight, "Ꝍ 2",   LabelAlignment.Center, Color.LightSteelBlue) );
            hbox.Add(new Label(boxWidth, hbox.RemainingHeight, "Ꝍ 3" ,  LabelAlignment.Center, Color.LightSteelBlue) );
            vbox.AddSpace(0, 4); 

            // Overcast 1,2,3
            hbox = vbox.AddLayoutHorizontalLineOfText();
            hbox.Add(Bnt_Overcast_Sel = new Label(boxWidth*3, Owner.TextFontDefault.Height," Overcasts - Clouds Opacity",LabelAlignment.Left));
            Bnt_Overcast_Sel.Click += new Action<Control, Point>(Bnt_Overcast_Sel_Click);
            hbox.Add(Overcast1 = new Label(boxWidth, hbox.RemainingHeight, "000", LabelAlignment.Center));
            hbox.Add(Overcast2 = new Label(boxWidth, hbox.RemainingHeight, "000", LabelAlignment.Center));
            hbox.Add(Overcast3 = new Label(boxWidth, hbox.RemainingHeight, "000", LabelAlignment.Center));
            vbox.AddSpace(0, 4); 
            vbox.AddHorizontalSeparator();

            // Precipitaion header
            hbox = vbox.AddLayoutHorizontalLineOfText();
            hbox.Add(new Label(boxWidth*3, Owner.TextFontDefault.Height," ",LabelAlignment.Left));
            hbox.Add(new Label(boxWidth, hbox.RemainingHeight, "X "  , LabelAlignment.Center, Color.LightSteelBlue) );
            hbox.Add(new Label(boxWidth, hbox.RemainingHeight, "Y ", LabelAlignment.Center, Color.LightSteelBlue) );
            hbox.Add(new Label(boxWidth, hbox.RemainingHeight, "Z " , LabelAlignment.Center, Color.LightSteelBlue) );
            vbox.AddSpace(0, 4); 
            
            // Precipitaion Wind vectors 1
            hbox = vbox.AddLayoutHorizontalLineOfText();
            hbox.Add(Bnt_PrecipWind1_Sel = new Label(boxWidth*3, Owner.TextFontDefault.Height," Precipitation 1 Wind",LabelAlignment.Left));
            Bnt_PrecipWind1_Sel.Click += new Action<Control, Point>(Bnt_PrecipWind1_Sel_Click);
            hbox.Add(PrecipWind1X = new Label(boxWidth, hbox.RemainingHeight, "000", LabelAlignment.Center));
            hbox.Add(PrecipWind1Y = new Label(boxWidth, hbox.RemainingHeight, "000", LabelAlignment.Center));
            hbox.Add(PrecipWind1Z = new Label(boxWidth, hbox.RemainingHeight, "000", LabelAlignment.Center));
            vbox.AddSpace(0, 4); 
            
            // Precipitaion Wind vectors 2
            hbox = vbox.AddLayoutHorizontalLineOfText();
            hbox.Add(Bnt_PrecipWind2_Sel = new Label(boxWidth*3, Owner.TextFontDefault.Height," Precipitation 2 Wind",LabelAlignment.Left));
            Bnt_PrecipWind2_Sel.Click += new Action<Control, Point>(Bnt_PrecipWind2_Sel_Click);
            hbox.Add(PrecipWind2X = new Label(boxWidth, hbox.RemainingHeight, "000", LabelAlignment.Center));
            hbox.Add(PrecipWind2Y = new Label(boxWidth, hbox.RemainingHeight, "000", LabelAlignment.Center));
            hbox.Add(PrecipWind2Z = new Label(boxWidth, hbox.RemainingHeight, "000", LabelAlignment.Center));
            vbox.AddSpace(0, 4); 
            
            // Precipitaion Particle size
            hbox = vbox.AddLayoutHorizontalLineOfText();
            hbox.Add(Bnt_PrecipParSize_Sel = new Label(boxWidth*3, Owner.TextFontDefault.Height," Precipitation Particle Size",LabelAlignment.Left));
            Bnt_PrecipParSize_Sel.Click   += new Action<Control, Point>(Bnt_PrecipParSize_Sel_Click);
            hbox.Add(PrecipParSize1 = new Label(boxWidth, hbox.RemainingHeight, "000", LabelAlignment.Center));
            
            // Precippitation density
            vbox.AddSpace(0, 4); 
            hbox = vbox.AddLayoutHorizontalLineOfText();
            hbox.Add(Bnt_Precip_Sel = new Label(boxWidth*3, Owner.TextFontDefault.Height," Precipitation Liquidit & Intensity",LabelAlignment.Left));
            Bnt_Precip_Sel.Click += new Action<Control, Point>(Bnt_Precip_Sel_Click);
            hbox.Add(PrecipLiquid    = new Label(boxWidth, hbox.RemainingHeight, "0", LabelAlignment.Center));
            hbox.Add(PricipIntPPSPM2 = new Label(boxWidth, hbox.RemainingHeight, "0", LabelAlignment.Center));
            hbox.Add(new Label(boxWidth, hbox.RemainingHeight, "Use Default keys", LabelAlignment.Center, Color.Gray));
            vbox.AddHorizontalSeparator();
            
            // Color/Brightness/Contrast Modifier header
            hbox = vbox.AddLayoutHorizontalLineOfText();
            hbox.Add(new Label(boxWidth*3, Owner.TextFontDefault.Height," ",LabelAlignment.Left));
            hbox.Add(new Label(boxWidth, hbox.RemainingHeight, "Desaturation"  , LabelAlignment.Center, Color.LightSteelBlue) );
            hbox.Add(new Label(boxWidth, hbox.RemainingHeight, "Brighness", LabelAlignment.Center, Color.LightSteelBlue) );
            hbox.Add(new Label(boxWidth, hbox.RemainingHeight, "Contrast" , LabelAlignment.Center, Color.LightSteelBlue) );
            vbox.AddSpace(0, 4); 
            
            // Vegetation Color/Brightness/Contrast Modifier
            hbox = vbox.AddLayoutHorizontalLineOfText();
            hbox.Add(Bnt_VegMod_Sel = new Label(boxWidth*3, Owner.TextFontDefault.Height," Vegetation",LabelAlignment.Left));
            Bnt_VegMod_Sel.Click += new Action<Control, Point>(Bnt_VegMod_Sel_Click);
            hbox.Add(VegDesatMod  = new Label(boxWidth, hbox.RemainingHeight, "000", LabelAlignment.Center));
            hbox.Add(VegBrightMod = new Label(boxWidth, hbox.RemainingHeight, "000", LabelAlignment.Center));
            hbox.Add(VegContMod   = new Label(boxWidth, hbox.RemainingHeight, "000", LabelAlignment.Center));
            vbox.AddSpace(0, 4); 
            
            // Terrain Color/Brightness/Contrast Modifier
            hbox = vbox.AddLayoutHorizontalLineOfText();
            hbox.Add(Bnt_TerrMod_Sel = new Label(boxWidth*3, Owner.TextFontDefault.Height," Scenery",LabelAlignment.Left));
            Bnt_TerrMod_Sel.Click += new Action<Control, Point>(Bnt_TerrMod_Sel_Click);
            hbox.Add(TerrDesatMod  = new Label(boxWidth, hbox.RemainingHeight, "000", LabelAlignment.Center));
            hbox.Add(TerrBrightMod = new Label(boxWidth, hbox.RemainingHeight, "000", LabelAlignment.Center));
            hbox.Add(TerrContMod   = new Label(boxWidth, hbox.RemainingHeight, "000", LabelAlignment.Center));
            vbox.AddSpace(0, 4); 
            vbox.AddHorizontalSeparator();
            vbox.AddSpace(0, 16); 
            
            // Reload textures and Save weather 
            hbox = vbox.AddLayoutHorizontalLineOfText();
            hbox.Add(Bnt_ReloadWeathertype_Sel = new Label(boxWidth*2, Owner.TextFontDefault.Height," ◄Reload Texture►",LabelAlignment.Left));
            Bnt_ReloadWeathertype_Sel.Click += new Action<Control, Point>(Bnt_ReloadWeathertype_Sel_Click);
            hbox.Add(Bnt_SaveWeathertype_Sel = new Label(boxWidth*2, Owner.TextFontDefault.Height," ◄Save Weather►",LabelAlignment.Left));
            Bnt_SaveWeathertype_Sel.Click += new Action<Control, Point>(Bnt_SaveWeathertype_Sel_Click);    
              
            Console.Write( "Weather Window Gui Created");
            ClearLabelColor();

            return vbox;
        }


        public void ClearLabelColor()
        {   
            // Scenery
            Bnt_ScFC_Sunrise_Sel.Color = Color_Unselected;
            Bnt_ScFC_Noon_Sel.Color    = Color_Unselected;
            Bnt_ScFC_Sunset_Sel.Color  = Color_Unselected;
            Bnt_ScViewDistance_Sel.Color = Color_Unselected;
            Bnt_SkyFC_Sunrise_Sel.Color = Color_Unselected;
            Bnt_SkyFC_Noon_Sel.Color    = Color_Unselected;
            Bnt_SkyFC_Sunset_Sel.Color  = Color_Unselected;
            Bnt_SkyViewDistance_Sel.Color = Color_Unselected;

            Bnt_Overcast_Sel.Color = Color_Unselected;
            Bnt_Wind_Sel.Color = Color_Unselected;

            ScFCSunrise_R.Color = Color_Unselected;
            ScFCSunrise_G.Color = Color_Unselected;
            ScFCSunrise_B.Color = Color_Unselected;
            ScFCNoon_R.Color    = Color_Unselected;
            ScFCNoon_G.Color    = Color_Unselected;
            ScFCNoon_B.Color    = Color_Unselected;
            ScFCSunset_R.Color  = Color_Unselected;
            ScFCSunset_G.Color  = Color_Unselected;
            ScFCSunset_B.Color  = Color_Unselected;

            ScVDSunrise.Color = Color_Unselected;
            ScVDNoon.Color    = Color_Unselected;
            ScVDSunset.Color  = Color_Unselected;
            ScVDPrecentMix.Color = Color_Unselected;

            // Sky
            SkyFCSunrise_R.Color = Color_Unselected;
            SkyFCSunrise_G.Color = Color_Unselected;
            SkyFCSunrise_B.Color = Color_Unselected;
            SkyFCNoon_R.Color    = Color_Unselected;
            SkyFCNoon_G.Color    = Color_Unselected;
            SkyFCNoon_B.Color    = Color_Unselected;
            SkyFCSunset_R.Color  = Color_Unselected;
            SkyFCSunset_G.Color  = Color_Unselected;
            SkyFCSunset_B.Color  = Color_Unselected;

            SkyVDSunrise.Color = Color_Unselected;
            SkyVDNoon.Color    = Color_Unselected;
            SkyVDSunset.Color  = Color_Unselected;
            SkyVDPrecentMix.Color  = Color_Unselected;

            WindSpeed.Color    = Color_Unselected;
            WindDirSky.Color   = Color_Unselected;

            Overcast1.Color = Color_Unselected;
            Overcast2.Color = Color_Unselected;
            Overcast3.Color = Color_Unselected;

            PrecipWind1X.Color = Color_Unselected;
            PrecipWind1Y.Color = Color_Unselected;
            PrecipWind1Z.Color = Color_Unselected;
            Bnt_PrecipWind1_Sel.Color = Color_Unselected;
            PrecipWind2X.Color = Color_Unselected;
            PrecipWind2Y.Color = Color_Unselected;
            PrecipWind2Z.Color = Color_Unselected;
            Bnt_PrecipWind2_Sel.Color = Color_Unselected; 

            PrecipParSize1.Color = Color_Unselected;
            //PrecipParSize2.Color = Color_Unselected; 
            Bnt_PrecipParSize_Sel.Color = Color_Unselected;
         
            Bnt_SkySunSize_Sel.Color = Color_Unselected;
            SkySunSizeSunrise.Color = Color_Unselected;
            SkySunSizeNoon.Color    = Color_Unselected;
            SkySunSizeSunset.Color  = Color_Unselected; 
            SkySunSizeCurrent.Color = Color_Unselected;

            VegDesatMod.Color  = Color_Unselected;
            VegBrightMod.Color = Color_Unselected;
            VegContMod.Color   = Color_Unselected;
            Bnt_VegMod_Sel.Color = Color_Unselected;
            TerrDesatMod.Color  = Color_Unselected;
            TerrBrightMod.Color = Color_Unselected;
            TerrContMod.Color   = Color_Unselected;
            Bnt_TerrMod_Sel.Color = Color_Unselected;

            SkyFCCurrent.Color = Color_Unselected; 
            ScFCCurrent.Color = Color_Unselected; 

            PrecipLiquid.Color    = Color_Unselected;
            PricipIntPPSPM2.Color = Color_Unselected;
            Bnt_Precip_Sel.Color  = Color_Unselected;

            Bnt_ReloadWeathertype_Sel.Color = Color_Unselected;
            Bnt_SaveWeathertype_Sel.Color = Color_Unselected;

        }

        // ------------- Callback section ------------------- 

        void Bnt_Precip_Sel_Click(Control arg1, Point arg2)
        {
            ClearLabelColor();
            EnmSelect = EnuSelection.Precip_Sel;
            PrecipLiquid.Color    = Color_Selected;    
            PricipIntPPSPM2.Color = Color_Selected; 
            Bnt_Precip_Sel.Color  = Color_Selected;
        }


        void Bnt_PrecipParSize_Sel_Click(Control arg1, Point arg2)
        {
            ClearLabelColor();
            EnmSelect = EnuSelection.PrecipParSize_Sel;
            PrecipParSize1.Color = Color_Selected;
            //PrecipParSize2.Color = Color_Selected; 
            Bnt_PrecipParSize_Sel.Color = Color_Selected;
        }

        void Bnt_VegMod_Sel_Click(Control arg1, Point arg2)
        {
            ClearLabelColor();
            EnmSelect = EnuSelection.VegMod_Sel;
            VegDesatMod.Color  = Color_Selected;
            VegBrightMod.Color = Color_Selected;
            VegContMod.Color   = Color_Selected;
            Bnt_VegMod_Sel.Color = Color_Selected;
        }

        void Bnt_TerrMod_Sel_Click(Control arg1, Point arg2)
        {
            ClearLabelColor();
            EnmSelect = EnuSelection.TerrMod_Sel;
            TerrDesatMod.Color  = Color_Selected;
            TerrBrightMod.Color = Color_Selected;
            TerrContMod.Color   = Color_Selected;
            Bnt_TerrMod_Sel.Color = Color_Selected;
        }

        void Bnt_PrecipWind1_Sel_Click(Control arg1, Point arg2)
        {
            ClearLabelColor();
            EnmSelect = EnuSelection.PrecipWind1_Sel;
            PrecipWind1X.Color = Color_Selected;
            PrecipWind1Y.Color = Color_Selected;
            PrecipWind1Z.Color = Color_Selected;
            Bnt_PrecipWind1_Sel.Color = Color_Selected;
        }

        void Bnt_PrecipWind2_Sel_Click(Control arg1, Point arg2)
        {
            ClearLabelColor();
            EnmSelect = EnuSelection.PrecipWind2_Sel;
            PrecipWind2X.Color = Color_Selected;
            PrecipWind2Y.Color = Color_Selected;
            PrecipWind2Z.Color = Color_Selected;
            Bnt_PrecipWind2_Sel.Color = Color_Selected;
        }

        void Bnt_SkySunSize_Sel_Click(Control arg1, Point arg2)
        {
            ClearLabelColor();
            EnmSelect = EnuSelection.SkySunSize_Sel;
            Bnt_SkySunSize_Sel.Color = Color_Selected;
            SkySunSizeSunrise.Color  = Color_Selected;
            SkySunSizeNoon.Color     = Color_Selected;
            SkySunSizeSunset.Color   = Color_Selected; 
            SkySunSizeCurrent.Color  = Color_Selected;
        }
 
        void Bnt_Overcast_Sel_Click(Control arg1, Point arg2)
        {
            ClearLabelColor();
            EnmSelect = EnuSelection.Overcast;
            Bnt_Overcast_Sel.Color = Color_Selected;
            Overcast1.Color = Color_Selected;
            Overcast2.Color = Color_Selected;
            Overcast3.Color = Color_Selected;
        }

        void Bnt_Wind_Sel_Click(Control arg1, Point arg2)
        {
            ClearLabelColor();
            Bnt_Wind_Sel.Color = Color_Selected;
            WindSpeed.Color = Color_Selected;
            WindDirSky.Color = Color_Selected;
            EnmSelect = EnuSelection.Wind_Sel;
        }

        void Bnt_ScViewDistance_sel_Click(Control arg1, Point arg2)
        {
            ClearLabelColor();
            Bnt_ScViewDistance_Sel.Color = Color_Selected;
            ScVDSunrise.Color = Color_Selected;
            ScVDNoon.Color    = Color_Selected;
            ScVDSunset.Color  = Color_Selected;
            ScVDPrecentMix.Color  = Color_Selected;
            EnmSelect = EnuSelection.ScViewDistance_Sel;
        }

        void Bnt_SkyViewDistance_sel_Click(Control arg1, Point arg2)
        {
            ClearLabelColor();
            Bnt_SkyViewDistance_Sel.Color = Color_Selected;
            SkyVDSunrise.Color = Color_Selected;
            SkyVDNoon.Color    = Color_Selected;
            SkyVDSunset.Color  = Color_Selected;
            SkyVDPrecentMix.Color = Color_Selected;
            EnmSelect = EnuSelection.SkyViewDistance_Sel;
        }

        void Bnt_ScFC_Sunrise_Sel_Click(Control arg1, Point arg2)
        {
            ClearLabelColor();
            Bnt_ScFC_Sunrise_Sel.Color = Color_Selected;
            ScFCSunrise_R.Color = Color_Selected;
            ScFCSunrise_G.Color = Color_Selected;
            ScFCSunrise_B.Color = Color_Selected;
            EnmSelect = EnuSelection.ScFC_Sunrise_Sel;
        }

        void Bnt_ScFC_Noon_Sel_Click(Control arg1, Point arg2)
        {
            ClearLabelColor();
            Bnt_ScFC_Noon_Sel.Color = Color_Selected;
            ScFCNoon_R.Color = Color_Selected;
            ScFCNoon_G.Color = Color_Selected;
            ScFCNoon_B.Color = Color_Selected;
            EnmSelect = EnuSelection.ScFC_Noon_Sel;
        }

        void Bnt_ScFC_Sunset_Sel_Click(Control arg1, Point arg2)
        {
            ClearLabelColor();
            Bnt_ScFC_Sunset_Sel.Color = Color_Selected;
            ScFCSunset_R.Color = Color_Selected;
            ScFCSunset_G.Color = Color_Selected;
            ScFCSunset_B.Color = Color_Selected;
            EnmSelect = EnuSelection.ScFC_Sunset_Sel;
        }

        void Bnt_SkyFC_Sunrise_Sel_Click(Control arg1, Point arg2)
        {
            ClearLabelColor();
            Bnt_SkyFC_Sunrise_Sel.Color = Color_Selected;
            SkyFCSunrise_R.Color = Color_Selected;
            SkyFCSunrise_G.Color = Color_Selected;
            SkyFCSunrise_B.Color = Color_Selected;
            EnmSelect = EnuSelection.SkyFC_Sunrise_Sel;
        }

        void Bnt_SkyFC_Noon_Sel_Click(Control arg1, Point arg2)
        {
            ClearLabelColor();
            Bnt_SkyFC_Noon_Sel.Color = Color_Selected;
            SkyFCNoon_R.Color = Color_Selected;
            SkyFCNoon_G.Color = Color_Selected;
            SkyFCNoon_B.Color = Color_Selected;
            EnmSelect = EnuSelection.SkyFC_Noon_Sel;
        }

        void Bnt_SkyFC_Sunset_Sel_Click(Control arg1, Point arg2)
        {
            ClearLabelColor();
            Bnt_SkyFC_Sunset_Sel.Color = Color_Selected;
            SkyFCSunset_R.Color = Color_Selected;
            SkyFCSunset_G.Color = Color_Selected;
            SkyFCSunset_B.Color = Color_Selected;
            EnmSelect = EnuSelection.SkyFC_Sunset_Sel;
        }

        void Bnt_ReloadWeathertype_Sel_Click(Control arg1, Point arg2)
        {   
            Console.Write( "\nWeather Texture Reloaded\n");
            Owner.Viewer.World.WeatherControl.ReloadWeatherSwitch = true;
        }
        void Bnt_SaveWeathertype_Sel_Click(Control arg1, Point arg2)
        {   
            Console.Write( "\nWeather Saved\n");
            Bnt_SaveWeathertype_Sel.Color = Color_Selected;
            Owner.Viewer.World.WeatherControl.SaveWeatherSwitch = true;
        }

    }
}
