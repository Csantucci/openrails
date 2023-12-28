Open Rails, NewYear version README - Rev.149.2
December 27th, 2023

Please note that the installation and use of Open Rails software, even of its unofficial versions, is governed by the Open Rails End User License Agreement. 

INSTALLATION
- the requirements for installation of the official Open Rails version apply;
- start openrails simply by clicking on Openrails.exe.


RELEASE NOTES
This unofficial version has been derived from the latest official Testing release T1.5.1-776, 
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
- Provide more tolerance for CentreOfGravity, see http://www.elvastower.com/forums/index.php?/topic/37366-centreofgravity/
- Map View dark modded, by ExRail, see http://www.elvastower.com/forums/index.php?/topic/37362-map-view-dark-moddded/
- Add curve force and brake cylinder pressure in sound debug window
- Sort of hack to fix http://www.elvastower.com/forums/index.php?/topic/37448-ammeter-configuration-steeringcarriages-emudmu/
- Re-instate and extend Precision and UpdateTime for SPEEDOMETER in cabvies, see http://www.elvastower.com/forums/index.php?/topic/37448-ammeter-configuration-steeringcarriages-emudmu/page__view__findpost__p__300651 
-  (ORNYMG only?) bug https://www.elvastower.com/forums/index.php?/topic/32640-or-newyear-mg/page__view__findpost__p__302120
- Tentative (ORNYMG only?) bug fix for https://www.elvastower.com/forums/index.php?/topic/32640-or-newyear-mg/page__view__findpost__p__302440
- features which are present in Unstable release or on Github but not yet in the publicly available testing release:
  *- allow for .sms sound specific for AI trains. see http://www.elvastower.com/forums/index.php?/topic/29878-specific-sound-for-ai-trains/page__view__findpost__p__295008 
  (PR #896); event numbers changed from 32n to 33n for compatibility with Testing release
  *- Additional parameters for Cruise Control, see https://blueprints.launchpad.net/or/+spec/additional-cruise-control-parameters (PR #839)
  *- (signalling) No speed update parameter, by roeter, see https://www.elvastower.com/forums/index.php?/topic/37573-proposed-new-signal-aspect-parameter-or-nospeedupdate/page__view__findpost__p__302320 (PR #889)
  *- (timetables) Allow depart early, by roeter, see https://www.elvastower.com/forums/index.php?/topic/36899-update-timetable-mode/page__view__findpost__p__296371 (PR #890) 
  *- Auto-save, by roeter, see https://www.elvastower.com/forums/index.php?/topic/35870-auto-save/page__view__findpost__p__280206 (PR #891)
  *- Signal Function OPP_SIG_ID_TRAINPATH, by roeter, see https://www.elvastower.com/forums/index.php?/topic/37572-proposed-new-signal-function-opp-sig-id-trainpath/page__view__findpost__p__302318 (PR #892)
  *- Fix signal errors, by roeter, see https://www.elvastower.com/forums/index.php?/topic/37570-debug-code/page__view__findpost__p__302315 (PR #893)
  *- Fix decrease colour bug in cabs, by roeter, see bug report https://bugs.launchpad.net/or/+bug/2043690 (PR #894)
  *- (timetables) Many options to define power and light related features, by roeter, see https://www.elvastower.com/forums/index.php?/topic/36899-update-timetable-mode/page__view__findpost__p__293894 (PR #895)
  *- NEW: ETCS DMI Updates, including 280 km/h max tacho setting by gpz (PR #900)
  *- NEW: Duplex steam engines - Booster Engine addition by steamer_ctn (PR #899)
  *- NEW: feat: supports switching adhesion precisions by cjakeman (PR #908 )
  *- NEW: Bug fix for https://www.elvastower.com/forums/index.php?/topic/37651-or-151-black-track-texture-why/ (PR #907)
  *- NEW: Bug fix for https://www.elvastower.com/forums/index.php?/topic/37685-potential-issue-with-north-american-v-scale-latest-7-ton-locosl/ (PR #906)
- NEW: Improvements to display features on web clients, by mbm_OR

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

