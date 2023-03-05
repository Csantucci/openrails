Open Rails, NewYear version README - Rev.139
March 5th, 2023

Please note that the installation and use of Open Rails software, even of its unofficial versions, is governed by the Open Rails End User License Agreement. 

INSTALLATION
- the requirements for installation of the official Open Rails version apply, with the precisions of next lines
- XNA 3.1 Redistributable is not needed
- you must have at least a Windows Vista computer. Windows XP is not supported
- start openrails simply by clicking on Openrails.exe.


RELEASE NOTES
This unofficial version has been derived from the latest official Testing release T1.5.1-340, 
plus some of the features already present in the Unstable release.


This version includes some features not (yet) available in the Open Rails testing official version, that is:
- addition of track sounds in the sound debug window (by dennisat)
- F5 HUD scrolling (by mbm_or)
- checkbox in General Options tab to enable or disable watchdog
- increase of remote horn sound volume level
- when car is selected through the F9 window, the car's brake line in the extended brake HUD is highlighted in yellow (by mbm_or)
- improved distance management in roadcar camera
- signal script parser (by perpetualKid): reduces CPU time needed for signal management
- general options checkbox for optional run at 32 bit on Win64 (consumes less memory, recommended for computers with 4 GB RAM)
- panto commands and animations now swapped when in rear cab
- correction to reduce transfer flickering at short distance
- option to skip saving commands (reduces save time on long activities), see http://www.elvastower.com/forums/index.php?/topic/33907-64-bit-openrails-consumes-more-memory/page__view__findpost__p__257687
- skip warning messages related to ruler token, that is introduced by TSRE5
- track gauge can be changed over the whole route, see http://www.elvastower.com/forums/index.php?/topic/34022-adjusting-track-gauge/
- re-introduced advanced coupling, by steamer_ctn
- bug fix for https://bugs.launchpad.net/or/+bug/1895391 Calculation of reversal point distance failing
- bug fix for http://www.elvastower.com/forums/index.php?/topic/34572-new-player-trains-dont-show-train-brakes-in-hud/
- bug fix for http://www.elvastower.com/forums/index.php?/topic/34633-unhandled-exception-overflow-in-win7/page__view__findpost__p__265463 , by mbm_OR
- tentative support for RAIN textures for objects, terrain and transfers
- dynamic terrain and scenery loading strategy (useful for slower computers)
- commands to selectively throw facing or trailing switches, see http://www.elvastower.com/forums/index.php?/topic/34991-opposite-switch-throwing-with-g-key/page__view__findpost__p__270291
- preliminary bug fix for http://www.elvastower.com/forums/index.php?/topic/35112-problem-with-tcs-scripts-and-timetable-mode/
- max fog distance increased to 300 km
- camera following detached wagons in hump yard operation by pressing Ctrl key while clicking with mouse on coupler in Train Operations Window to uncouple wagon
- bug fix that didn't check for null label text, see http://www.elvastower.com/forums/index.php?/topic/32640-or-newyear-mg/page__view__findpost__p__273496
- Various fixes related to reversals and couplings, see e.g. http://www.elvastower.com/forums/index.php?/topic/29383-assistance-with-ai-shunting/page__view__findpost__p__277369
- Insert cabview controls in side views of 2D cabs, see http://www.elvastower.com/forums/index.php?/topic/35640-inserting-cabview-controls-in-side-views-of-2d-cabs/ 
- workaround for http://www.elvastower.com/forums/index.php?/topic/32640-or-newyear-mg/page__view__findpost__p__278074 
- "ORNYMG only" option tab, including all option checkboxes available only in ORNYMG
- kept Locomotive() TCS script hook
- kept various options in ORNYMG only tab
- add PERCENT UoM to ORTS_SIGNED_TRACTION_TOTAL_BRAKING
- add cabview controls ODOMETER_UP and ODOMETER_DOWN
- Improvements in sky color and in other environment areas, by DR-Aeronautics
- multiple preset viewpoints for 3D cabs, see http://www.elvastower.com/forums/index.php?/topic/36339-multiple-preset-viewpoints-for-3d-cabs/
- fix crash when reinitializing EOTs on a carless train
- keyboard commands for Overcharge and Quick Release, by cesarBLG
- NEW: Enable rotation of the DPU table, see http://www.elvastower.com/forums/index.php?/topic/26249-remote-control-dpu-units/page__view__findpost__p__294403
- NEW: fix two brake values in DPU table, see http://www.elvastower.com/forums/index.php?/topic/26249-remote-control-dpu-units/page__view__findpost__p__294519
- NEW: allow for .sms sound specific for AI trains
- features which are present in Unstable release or on Github but not yet in the publicly available testing release:
  - Unify Raildriver code implementation, by cesarBLG; includes management of external devices different from Raildriver, plus two minor RD  code fixes (PR #757)
  - NEW: fix: Stop z-fighting by pushing world/view/projection multiplications onto the GPU, by James Ross (PR #807) 


Info about content of the various PR to the Unstable release can be found here
https://github.com/openrails/openrails/pulls


The Monogame related code intentionally coincides only partly with the code of the OR official testing version.

CREDITS
This unofficial version couldn't have been created without following contributions:
- the whole Open Rails Development Team and Open Rails Management Team, that have generated the official Open Rails version
- the Monogame Development Team
- Peter Gulyas, who created the first Monogame version of Open Rails
- perpetualKid
- Jindrich
- Dennis A T (dennisat)
- DR-Aeronautics
- Mauricio (mbm_OR)
- cjakeman
- BillC
- Peter Newell (steamer_ctn)
- Rob Roeterdink (roeter)
- jonas
- YoRyan
- césarbl
- Paolo
- Weter
- Carlo Santucci

- all those who contributed with ideas and provided contents for testing and pointed to malfunctions.

DISCLAIMER
No testing on a broad base of computer configurations and of contents has been done. Therefore, in addition
to the disclaimers valid also for the official Open Rails version, 
the above named persons keep no responsibility, including on malfunctions, damages, losses of data or time.
It is reminded that Open Rails is distributed WITHOUT ANY WARRANTY, and without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU General Public License for more details.

