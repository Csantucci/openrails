Open Rails, NewYear version README - Rev.146
August 30th, 2023

Please note that the installation and use of Open Rails software, even of its unofficial versions, is governed by the Open Rails End User License Agreement. 

INSTALLATION
- the requirements for installation of the official Open Rails version apply;
- start openrails simply by clicking on Openrails.exe.


RELEASE NOTES
This unofficial version has been derived from the latest official Testing release T1.5.1-553, 
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
- workaround for http://www.elvastower.com/forums/index.php?/topic/32640-or-newyear-mg/page__view__findpost__p__278074 
- "ORNYMG only" option tab, including all option checkboxes available only in ORNYMG
- kept Locomotive() TCS script hook
- kept various options in ORNYMG only tab
- add PERCENT UoM to ORTS_SIGNED_TRACTION_TOTAL_BRAKING
- add cabview controls ODOMETER_UP and ODOMETER_DOWN
- Improvements in sky color and in other environment areas, by DR-Aeronautics
- fix crash when reinitializing EOTs on a carless train
- keyboard commands for Overcharge and Quick Release, by cesarBLG
- allow for .sms sound specific for AI trains. see http://www.elvastower.com/forums/index.php?/topic/29878-specific-sound-for-ai-trains/page__view__findpost__p__295008
- enable modifying and compiling the .fx shader files, see http://www.elvastower.com/forums/index.php?/topic/36968-mgfxwhywhen-nov-2022what-does-a-user-do-now/page__view__findpost__p__295571
- accept ORTSAirBrakeMainResVolume as .eng files token, used in some TrainSimulations routes (see https://www.trainsim.com/forums/forum/open-rails/open-rails-discussion/2284903-shasta-route-available?p=2285365#post2285365)
- Provide more tolerance for CentreOfGravity, see http://www.elvastower.com/forums/index.php?/topic/37366-centreofgravity/
- Map View dark modded, by ExRail, see http://www.elvastower.com/forums/index.php?/topic/37362-map-view-dark-moddded/
- features which are present in Unstable release or on Github but not yet in the publicly available testing release:
  - Additional parameters for Cruise Control, see https://blueprints.launchpad.net/or/+spec/additional-cruise-control-parameters (PR #839)
  - Unify Raildriver code implementation, by cesarBLG; includes management of external devices different from Raildriver, plus two minor RD  code fixes (PR #757)
  - Animating locomotive and carriage windows, see http://www.elvastower.com/forums/index.php?/topic/37120-opening-and-closing-trainset-windows/ (PR #841)
  - Fix engine air leak problem, by cesarBLG, see http://www.elvastower.com/forums/index.php?/topic/37388-acela-hhp-8-engineering/page__p__299632#entry299632 (PR #862)
  - multiple preset viewpoints for 3D cabs, see http://www.elvastower.com/forums/index.php?/topic/36339-multiple-preset-viewpoints-for-3d-cabs/ (PR #863)
  - NEW: Update in addition of air flow meters, by SteelFill, see http://www.elvastower.com/forums/index.php?/topic/34527-wishes-for-improvement-of-braking-systems/page__st__570__p__299318#entry299318 (PR #857); air flow indication now available also in DPU displays
  - NEW: Update for fix for steam adhesion bug, by Peter Newell, see http://www.elvastower.com/forums/index.php?/topic/37327-issues-with-advanced-adhesion/ (PR #859)
  - NEW: Update for changes in Car Operaions Menu re braking, by sweiland, see http://www.elvastower.com/forums/index.php?/topic/37062-proposal-f9-train-operations/page__p__296373__hl__%2Btrain+%2Boperations__fromsearch__1#entry296373 (PR #860)
  - NEW: Fixes for undesired emergency applications, by SteelFill, see http://www.elvastower.com/forums/index.php?/topic/34527-wishes-for-improvement-of-braking-systems/page__st__580__p__299828#entry299828 (PR #864)
  - NEW: Fix articulation for 0-axis traincars by SteelFill, see http://www.elvastower.com/forums/index.php?/topic/37404-possible-issue-with-articulated-wagons-with-0-axles/ (PR #866)
- NEW: Fix for exhaust of non-lead locomotives, see  http://www.elvastower.com/forums/index.php?/topic/32640-or-newyear-mg/page__view__findpost__p__299968
- NEW: Fix ORNYMG bug disabling track curve sounds, see http://www.elvastower.com/forums/index.php?/topic/32640-or-newyear-mg/page__view__findpost__p__299995
- NEW: Add curve force and brake cylinder pressure in sound debug window










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
- Mauricio (mbm_OR)
- BillC
- cjakeman
- James Ross
- Peter Newell (steamer_ctn)
- jonas
- YoRyan
- césarbl
- Paolo
- Weter
- sweiland
- SteelFill
- ExRail
- Carlo Santucci

- all those who contributed with ideas and provided contents for testing and pointed to malfunctions.

DISCLAIMER
No testing on a broad base of computer configurations and of contents has been done. Therefore, in addition
to the disclaimers valid also for the official Open Rails version, 
the above named persons keep no responsibility, including on malfunctions, damages, losses of data or time.
It is reminded that Open Rails is distributed WITHOUT ANY WARRANTY, and without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU General Public License for more details.

