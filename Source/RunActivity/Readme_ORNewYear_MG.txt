Open Rails, NewYear version README - Rev.156
September 9th, 2024

Please note that the installation and use of Open Rails software, even of its unofficial versions, is governed by the Open Rails End User License Agreement. 

INSTALLATION
- the requirements for installation of the official Open Rails version apply;
- start openrails simply by clicking on Openrails.exe.


RELEASE NOTES
This unofficial version has been derived from the latest official Testing release T1.5.1-1192, 
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
- fix crash when reinitializing EOTs on a carless train
- enable modifying and compiling the .fx shader files, see http://www.elvastower.com/forums/index.php?/topic/36968-mgfxwhywhen-nov-2022what-does-a-user-do-now/page__view__findpost__p__295571
- accept ORTSAirBrakeMainResVolume as .eng files token, used in some TrainSimulations routes (see https://www.trainsim.com/forums/forum/open-rails/open-rails-discussion/2284903-shasta-route-available?p=2285365#post2285365)
- Add curve force and brake cylinder pressure in sound debug window
- Sort of hack to fix http://www.elvastower.com/forums/index.php?/topic/37448-ammeter-configuration-steeringcarriages-emudmu/
- Re-instate and extend Precision and UpdateTime for SPEEDOMETER in cabvies, see http://www.elvastower.com/forums/index.php?/topic/37448-ammeter-configuration-steeringcarriages-emudmu/page__view__findpost__p__300651 
-  (ORNYMG only?) bug https://www.elvastower.com/forums/index.php?/topic/32640-or-newyear-mg/page__view__findpost__p__302120
- Tentative (ORNYMG only?) bug fix for https://www.elvastower.com/forums/index.php?/topic/32640-or-newyear-mg/page__view__findpost__p__302440
- (signalling) No speed update parameter, by roeter, see https://www.elvastower.com/forums/index.php?/topic/37573-proposed-new-signal-aspect-parameter-or-nospeedupdate/page__view__findpost__p__302320
- (timetables) Many options to define power related features, by roeter, see https://www.elvastower.com/forums/index.php?/topic/36899-update-timetable-mode/page__view__findpost__p__293894
- Timetable: player train switching, open/close doors on AI trains and some minor extra features, see https://www.elvastower.com/forums/index.php?/topic/37710-autopilot-for-timetable-mode/
- Disable Polach adhesion, see here https://www.elvastower.com/forums/index.php?/topic/32640-or-newyear-mg/page__view__findpost__p__305613 and following posts
- Fix for https://bugs.launchpad.net/or/+bug/2057708 ORTSScreenPage parameter doesn't work for Dials
- Bug fix for https://www.elvastower.com/forums/index.php?/topic/37911-timetable-crashing-for-no-reason/
- Fix 3Dcab digits alignment, see https://www.elvastower.com/forums/index.php?/topic/24040-3d-cabs/page__view__findpost__p__307374
- Bug fix for https://www.elvastower.com/forums/index.php?/topic/37948-or-timetable-error/page__view__findpost__p__307975
- Fix bug of impossibility to clear signals in dispatcher window
- Workaround for https://www.elvastower.com/forums/index.php?/topic/38038-crash-on-attaching-to-an-ai-train/
- Conditionally correct invalid values of TrainPipeLeakRate and Auxiliary Reservoir Volume
- Add slider to set wind variability in ORNYMG only options
- fix for https://www.elvastower.com/forums/index.php?/topic/32640-or-newyear-mg/page__view__findpost__p__309405
- fix for https://www.elvastower.com/forums/index.php?/topic/38126-signal-off-when-approach-on-platform/
- features which are present in Unstable release or on Github but not yet in the publicly available testing release:
  *- allow for .sms sound specific for AI trains. see http://www.elvastower.com/forums/index.php?/topic/29878-specific-sound-for-ai-trains/page__view__findpost__p__295008 
  (PR #896); event numbers changed from 32n to 33n for compatibility with Testing release
  *- Auto-save, by roeter, see https://www.elvastower.com/forums/index.php?/topic/35870-auto-save/page__view__findpost__p__280206 (PR #891)
  *- Signal Function OPP_SIG_ID_TRAINPATH, by roeter, see https://www.elvastower.com/forums/index.php?/topic/37572-proposed-new-signal-function-opp-sig-id-trainpath/page__view__findpost__p__302318 (PR #892)
  *- ETCS DMI Updates, including 280 km/h max tacho setting by gpz (PR #900)
  *- Added mouse wheel support for controls which can be moved by pressing the left mouse button, by sweiland (PR #919)
  *- Add curve squeal to route, by steamer_CTN (PR #923)
  *- Consolidated wind simulation, by James Ross (PR #799)
  *- Avoid pulsing graphics part 2, by James Ross, see https://www.elvastower.com/forums/index.php?/topic/37592-lags-by-mg-with-route-24-gb/ (PR #952)
  *- fix pantographs on unpowered cars, by cesarBLG (PR #962)
  *- Bug fix for https://www.elvastower.com/forums/index.php?/topic/29798-openclose-doors-in-ai-trains/page__view__findpost__p__310272 AI train starts with open doors (PR #974)
  *- Fix Trackviewer crash on big zoom value  (master PR  #959 by Fred-si)
  *- Fix Lights Crash on Corrupt Shapes (PR #953 by steelfill)
  *- NEW: merge Downloading route content (Github, zip) second part branch, by sweiland (PR #980)
- NEW: multiple lightglow types, see https://www.elvastower.com/forums/index.php?/topic/38247-actual-lightglowpng-generates-too-small-lights-in-existing-trainsets/   


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
- roeter
- Carlo Santucci

- all those who contributed with ideas and provided contents for testing and pointed to malfunctions.

DISCLAIMER
No testing on a broad base of computer configurations and of contents has been done. Therefore, in addition
to the disclaimers valid also for the official Open Rails version, 
the above named persons keep no responsibility, including on malfunctions, damages, losses of data or time.
It is reminded that Open Rails is distributed WITHOUT ANY WARRANTY, and without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU General Public License for more details.

