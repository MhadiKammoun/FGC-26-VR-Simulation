using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using TMPro;

public class MatchFlowManager : MonoBehaviour
{
    [Header("Input System Actions")]
    [Tooltip("InputActionReference for pausing/unpausing (e.g., Keyboard Escape or Gamepad Start/Options).")]
    [SerializeField] private InputActionReference pauseAction;

    [Header("Flow UI Panels")]
    [SerializeField] private GameObject welcomePanel;
    [SerializeField] private GameObject setupStatusPanel;
    [SerializeField] private GameObject matchHudPanel;
    [SerializeField] private GameObject pauseMenuPanel;
    [SerializeField] private GameObject controlsPanel;
    [SerializeField] private GameObject resultsPanel;

    [Header("Flow Buttons")]
    [SerializeField] private Button setupFieldButton;
    [SerializeField] private Button startMatchButton;
    [SerializeField] private Button hudPauseButton;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button resultsPlayAgainButton;

    [Header("Controls Sub-Menu Buttons")]
    [SerializeField] private Button openControlsButton;
    [SerializeField] private Button closeControlsButton;

    [Header("Setup Screen UI Elements")]
    [SerializeField] private TextMeshProUGUI countdownTimerText;
    [SerializeField] private TextMeshProUGUI gameFactText;
    [SerializeField] private Slider setupProgressBar;

    [Header("Results Screen UI Elements")]
    [SerializeField] private TextMeshProUGUI resultsFinalScoreText;
    [SerializeField] private TextMeshProUGUI resultsBreakdownText;
    [SerializeField] private TextMeshProUGUI settlingNoticeText;

    [Header("Countdown Visual Colors")]
    [SerializeField] private Color countdownStartColor = new Color(0.2f, 1f, 0.3f);
    [SerializeField] private Color countdownMidColor = new Color(1f, 0.85f, 0.1f);
    [SerializeField] private Color countdownEndColor = new Color(1f, 0.25f, 0.25f);
    [SerializeField] private Color readyTextColor = new Color(0f, 0.9f, 1f);

    [Header("Systems & Spawners")]
    [SerializeField] private MatchTimer matchTimer;
    [SerializeField] private SphereSpawner sphereSpawner;
    [SerializeField] private ScoreManagerUI scoreManager;
    [SerializeField] private ClimbDetector climbDetector;
    [SerializeField] private Animator wallSpawnerAnimator;

    [Header("GameObjects To Control During Matches")]
    [SerializeField] private GameObject[] objectsToControl;
    [SerializeField] private bool includeChildren = true;

    [Header("Setup & Post-Match Timings")]
    [SerializeField] private float fieldSetupRealDuration = 12f;
    [SerializeField] private float setupSimulationSpeed = 3f;
    [SerializeField] private float tipCycleInterval = 3.5f;
    [SerializeField] private float settlingDuration = 5.0f;

    [Header("Official Game Lore & Match Strategy Tips")]
    [TextArea(2, 4)]
    [SerializeField]
    private string[] gameFacts = new string[]
    {
        // --- Scoring & Multipliers ---
        "Tip: Every WILDFIRE ball contained inside your Regional Alliance Suppression Unit awards one base point.",
        "Tip: Suppression Unit points are scaled by your Alliance Climb Multiplier, dramatically boosting your endgame match score.",
        "Tip: The Extinguisher is a shared scoring structure—each ball deposited awards one point directly to all six teams.",
        "Tip: Points earned from the Extinguisher bypass the Regional Climb Multiplier, adding directly to the total score.",
        "Tip: The Climb Multiplier calculates as one plus the sum of each robot's individual zone increment.",
        "Tip: A robot contacting Zone 1 while touching the field surface adds 0.05 to the Climb Multiplier.",
        "Tip: Fully elevating off the carpet in Zone 1 awards a 0.10 multiplier bonus to your Regional Alliance.",
        "Tip: Reaching a fully suspended hang in Zone 2 grants a 0.20 boost to your alliance's Climb Multiplier.",
        "Tip: Reaching Zone 3 while fully off the playing carpet adds 0.30 to your alliance's Climb Multiplier.",
        "Tip: All three alliance robots suspended in Zone 3 yield the maximum Regional Climb Multiplier of 1.90.",
        "Tip: All fractional match scores round up to the nearest whole integer during final tally calculations.",
        "Tip: A robot that completely lifts a partner robot off the floor earns twenty-five Partner Climb points.",
        "Tip: One robot can legally lift both alliance partners, earning up to fifty Partner Climb points.",
        "Tip: Uniting four total robots across both alliances in Zone 3 earns ten Coopertition Bonus points.",
        "Tip: Suspending five robots across both alliances in Zone 3 awards twenty-five shared Coopertition Bonus points.",
        "Tip: A six-robot cooperative summit in Zone 3 grants the maximum forty-point Coopertition Bonus to everyone.",
        "Tip: Robots must cross through Zone 1 first; directly latching onto Zone 2 or 3 invalidates multiplier scoring.",
        "Tip: If your robot spans across multiple brace zones, you only earn the multiplier of the lowest zone contacted.",
        "Tip: The tape boundary line counts as the upper limit of the lower zone; cross it completely!",
        "Tip: Balls trapped inside robot mechanisms, chutes, or loose on the carpet at match end award zero points.",
        "Tip: Carried partner robots only earn Zone 2 or 3 multipliers if attached before leaving Zone 1.",
        "Tip: The Suppression Unit canopy opening has a 165 cm clearance—calibrate your high dumper or shooter arc accordingly.",
        "Tip: Real-time scores displayed during the match are unofficial; field refs manually certify final tallies after resting.",
        "Tip: Tiebreakers in ranking rely first on high match scores, then on cumulative unmultiplied Suppression Unit points.",
        "Tip: Only undamaged WILDFIRE game pieces will be counted toward your alliance's final match score.",

        // --- Field Navigation & Robot Handling ---
        "Tip: Over five hundred foam WILDFIRE balls carpet the arena floor; prioritize high ground clearance to prevent high-centering.",
        "Tip: Keep your center of gravity low when traversing deep ball clusters to prevent tipping over.",
        "Tip: Compliant intake rollers prevent jamming when simultaneously collecting multiple 100 mm polyurethane foam balls.",
        "Tip: The field carpet reacts like standard commercial low-pile flooring; tune your drivetrain drift and turning scrubbing accordingly.",
        "Tip: Be aware of the galvanized steel braces angled from 24 cm up to 197 cm high across the arena.",
        "Tip: Steel climb pipes can deflect up to 100 mm under heavy loads; build flexible, forgiving clambering hooks.",
        "Tip: Use five field AprilTags mounted on scoring unit faces to calibrate vision-based auto-alignment and odometry.",
        "Tip: Intake paths should measure wider than 100 mm to accommodate slight manufacturing variations in foam ball diameters.",
        "Tip: Robots must begin within a compact 50 cm cube volume before expanding horizontally during match play.",
        "Tip: Horizontal extensions are strictly limited to 50 cm in only one single direction at any time.",
        "Tip: While horizontal extensions are restricted, there is no maximum limit imposed on your robot's vertical reach.",
        "Tip: Design quick passive releases for climb hooks; all game pieces and field attachments must release without electrical power.",
        "Tip: Steer clear of high-density ball piles near field corners to avoid wedging your chassis against perimeter guardrails.",
        "Tip: Ball dispersal under the central Extinguisher creates immediate congestion right in front of the primary scoring goals.",
        "Tip: Funneling balls into the narrow Fire Shield Port requires smooth bumper geometry and accurate approach angles.",
        "Tip: Polyurethane foam game pieces compress under pressure; ensure your internal conveyors apply consistent grip without stalling motors.",
        "Tip: Guardrails stand 20 cm tall; design your chassis bumpers to absorb boundary collisions without riding over walls.",
        "Tip: The field is elevated 70 cm off the venue floor; never drive erratically near structural perimeter boundaries.",
        "Tip: Fast sweeping intakes allow your robot to bulldoze loose balls directly toward your human player's Fire Shield.",
        "Tip: Maintain balanced weight distribution to keep your drivetrain from teetering while traversing dense rolling game pieces.",
        "Tip: Practice driving over carpet heavily strewn with loose foam balls to master unpredictable slippage and trajectory loss.",
        "Tip: Check intake wheel compliance regularly; scuffed or dusty rollers lose friction against smooth polyurethane game spheres.",
        "Tip: Avoid hard snag points on your chassis underbelly to prevent loose foam balls from dragging beneath you.",
        "Tip: Position your cameras with clear sightlines above the 50 cm baseline to reliably track AprilTags above ball piles.",
        "Tip: Rigid chassis structures ensure reliable climbs when ascending the steep, slippery heat-shrink covered steel pipe braces.",

        // --- Match Flow & Penalties ---
        "Tip: Each match runs exactly two minutes and thirty seconds; plan transitions between cycling and climbing carefully.",
        "Tip: Controllers must be placed onto the playing surface immediately once the match timer counts down to zero.",
        "Tip: Any intentional robot movement after the match timer expires will result in an immediate Yellow Card.",
        "Tip: Drive teams must remain fully inside their designated Alliance Station perimeter boundaries throughout the entire match.",
        "Tip: Only the pre-designated Human Player may enter the tape-marked Human Player Zone to interact with field elements.",
        "Tip: Human Players must stand strictly inside the Human Player Zone while scoring balls or operating the lever.",
        "Tip: Minor fouls add five percent of your alliance's unpenalized score to your opponent's final match tally.",
        "Tip: Major fouls award ten percent of your regional alliance's total score directly to the opposing alliance.",
        "Tip: Minor and major fouls accumulate cumulatively and are calculated from your pre-penalty baseline alliance match score.",
        "Tip: Never reach into the arena until Suppression Unit and Extinguisher LEDs glow green to signal safe entry.",
        "Tip: Preloading foam WILDFIRE balls into your robot before the match starts is strictly prohibited by field regulations.",
        "Tip: If an unmoving robot needs assistance within the first 30 seconds, only power, battery, and loose wires may be touched.",
        "Tip: Touching an operable robot during the match outside authorized safety windows results in an immediate robot disablement.",
        "Tip: Intentionally launching or shooting game pieces at humans, robots, or outside the arena incurs an immediate Red Card.",
        "Tip: Human players are strictly forbidden from depositing WILDFIRE balls directly into the Regional Suppression Units.",
        "Tip: Never grasp, clamp, or hang from arena guardrails, Extinguisher frames, or regional Fire Shield structures.",
        "Tip: Forcing an opposing robot into committing an unintentional foul transfers that violation penalty directly to your alliance.",
        "Tip: Drive teams may not communicate with external audience spotters or use remote wireless coaching aids during gameplay.",
        "Tip: Receiving a Red Card awards zero match points and eliminates that match from being your dropped lowest round.",
        "Tip: A team failing to send at least a Human Player to the arena receives an automatic White Card.",
        "Tip: Scoring results are evaluated during a five-second settling period after the clock hits zero; remain stationary!",
        "Tip: Robots must start the match placed inside their Regional Zone and in physical contact with the perimeter guardrail.",
        "Tip: Human players can only pass balls to robots or carpet via the gravity chute by pressing the lever.",
        "Tip: Deliberately damaging, deforming, or shredding WILDFIRE foam balls leads to cards and uncounted match game pieces.",
        "Tip: Avoid extending mechanisms beyond the single-direction 50 cm limit to evade severe Yellow Card and Major Foul penalties.",

        // --- Strategic Mindset & Teamwork ---
        "Tip: Wildfire containment is an offensive challenge—defensive pinning, blocking, and scoring interference draw immediate Major Fouls.",
        "Tip: Coordinate designated roles early: assign primary floor sweepers, Fire Shield shuttle feeders, and dedicated high-altitude climbers.",
        "Tip: Feeding balls through the Fire Shield Port enables your Human Player to score uncontested Extinguisher global points.",
        "Tip: Any robot can feed either Fire Shield, making cross-field ball delivery a versatile strategy for global scoring.",
        "Tip: Begin ascending the steel brace with at least thirty seconds remaining to secure stable multiplier positions.",
        "Tip: Because three robots share a single brace, practice coordinated climb sequences to avoid tangling or blocking teammates.",
        "Tip: Unintentional falls on the shared brace are not penalized, but solid anti-backdrive mechanical locks keep everyone secure.",
        "Tip: Your Captain should actively track match time, communicate alliance cycles, and call the final climb sequence.",
        "Tip: Work closely with the opposing alliance early to coordinate reaching four or more robots in Zone 3 for bonus points.",
        "Tip: Heavy ball hoppers save travel time, as there is no holding capacity limit for robots or human players.",
        "Tip: In playoff alliances of four teams, every member must play in at least one elimination match round.",
        "Tip: Build rapport with alliance partners before queuing to synchronize autonomous routines and field starting positions.",
        "Tip: Rapid Fire Shield cycling lets human players safely clear field congestion while racking up steady Extinguisher points.",
        "Tip: If your climber falters and touches the carpet while in upper zones, immediately re-traverse from Zone 1.",
        "Tip: Smooth driver pacing beats erratic sprinting; consistent cycles prevent disastrous robot rollovers in crowded field traffic.",
        "Tip: Partner Climbs award twenty-five points each—designing a deployable lift bar can win tight playoff matches.",
        "Tip: Keep your pit workspace organized; prompt robot turnarounds ensure you never delay match schedules or draw penalties.",
        "Tip: If partner robots lack climbing mechanisms, clear the lower brace so high-tier climbers can traverse unimpeded.",
        "Tip: Balance regional ball scoring against the global Extinguisher; total ranking success demands excellence in both elements.",
        "Tip: Establish crisp, concise non-verbal drive team signals to communicate over loud, energetic arena spectator noise.",
        "Tip: Use practice matches to verify controller bindings, calibrate sensor thresholds, and inspect structural mechanical fasteners.",
        "Tip: Remember Coopertition: helping rival alliances optimize their Zone 3 climbs elevates the entire tournament's point potential.",
        "Tip: Inspect chassis wheels after every match to clean out collected carpet fibers and maintain optimal driving traction.",
        "Tip: Treat all participants, volunteers, and officials with Gracious Professionalism—integrity defines true competitive excellence.",
        "Tip: United global teamwork extinguishes the fiercest wildfire; communicate, adapt quickly, and ignite your team's innovation!"
    };

    private float originalFixedDeltaTime;
    private float prePauseTimeScale = 1f;
    private bool isPaused = false;
    private bool isSettingUp = false;
    private bool matchStarted = false;
    private bool isSettling = false;

    public bool IsPaused => isPaused;
    public bool MatchStarted => matchStarted;

    private void Awake()
    {
        originalFixedDeltaTime = Time.fixedDeltaTime;
    }

    private void OnEnable()
    {
        if (pauseAction != null && pauseAction.action != null)
        {
            pauseAction.action.Enable();
            pauseAction.action.performed += OnPauseActionTriggered;
        }
    }

    private void OnDisable()
    {
        if (pauseAction != null && pauseAction.action != null)
        {
            pauseAction.action.performed -= OnPauseActionTriggered;
            pauseAction.action.Disable();
        }

        ResetTimeScale();
        AudioListener.pause = false;
    }

    private void Start()
    {
        SetTargetScriptsActive(false);

        if (welcomePanel != null) welcomePanel.SetActive(true);
        if (setupStatusPanel != null) setupStatusPanel.SetActive(false);
        if (matchHudPanel != null) matchHudPanel.SetActive(false);
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        if (controlsPanel != null) controlsPanel.SetActive(false);
        if (resultsPanel != null) resultsPanel.SetActive(false);
        if (startMatchButton != null) startMatchButton.gameObject.SetActive(false);

        if (setupFieldButton != null) setupFieldButton.onClick.AddListener(OnSetupFieldClicked);
        if (startMatchButton != null) startMatchButton.onClick.AddListener(OnStartMatchClicked);
        if (hudPauseButton != null) hudPauseButton.onClick.AddListener(TogglePause);
        if (resumeButton != null) resumeButton.onClick.AddListener(UnpauseGame);
        if (restartButton != null) restartButton.onClick.AddListener(ReloadCurrentScene);
        if (resultsPlayAgainButton != null) resultsPlayAgainButton.onClick.AddListener(ReloadCurrentScene);

        // Controls sub-menu buttons
        if (openControlsButton != null) openControlsButton.onClick.AddListener(OpenControlsSubPanel);
        if (closeControlsButton != null) closeControlsButton.onClick.AddListener(CloseControlsSubPanel);

        if (matchTimer != null) matchTimer.OnMatchTimerEnd += HandleMatchEnded;
    }

    private void OnDestroy()
    {
        if (matchTimer != null) matchTimer.OnMatchTimerEnd -= HandleMatchEnded;
        ResetTimeScale();
        AudioListener.pause = false;
    }

    private void OnPauseActionTriggered(InputAction.CallbackContext context)
    {
        if (!matchStarted || isSettling) return;

        // If currently in the Controls sub-panel, pressing Pause acts as a "Back" button
        if (controlsPanel != null && controlsPanel.activeSelf)
        {
            CloseControlsSubPanel();
            return;
        }

        TogglePause();
    }

    #region Field Setup & Match Start

    private void OnSetupFieldClicked()
    {
        isSettingUp = true;
        if (setupFieldButton != null) setupFieldButton.interactable = false;
        if (welcomePanel != null) welcomePanel.SetActive(false);
        if (setupStatusPanel != null) setupStatusPanel.SetActive(true);

        if (wallSpawnerAnimator != null)
        {
            wallSpawnerAnimator.SetTrigger("DeployWalls");
        }

        SetTimeScale(setupSimulationSpeed);

        if (sphereSpawner != null) sphereSpawner.StartSpawningField();

        StartCoroutine(FieldSetupCountdownRoutine());
        StartCoroutine(CycleFactsRoutine());
    }

    private IEnumerator FieldSetupCountdownRoutine()
    {
        float remaining = fieldSetupRealDuration;

        while (remaining > 0f)
        {
            if (!isPaused)
            {
                remaining -= Time.unscaledDeltaTime;
                float progress = 1f - Mathf.Clamp01(remaining / fieldSetupRealDuration);

                if (setupProgressBar != null)
                    setupProgressBar.value = progress;

                if (countdownTimerText != null)
                {
                    int secondsDisplay = Mathf.CeilToInt(Mathf.Max(0, remaining));
                    countdownTimerText.SetText("{0}s", secondsDisplay);

                    if (progress < 0.5f)
                        countdownTimerText.color = Color.Lerp(countdownStartColor, countdownMidColor, progress * 2f);
                    else
                        countdownTimerText.color = Color.Lerp(countdownMidColor, countdownEndColor, (progress - 0.5f) * 2f);
                }
            }

            yield return null;
        }

        isSettingUp = false;

        if (!isPaused) ResetTimeScale();

        if (setupProgressBar != null) setupProgressBar.value = 1f;

        if (countdownTimerText != null)
        {
            countdownTimerText.color = readyTextColor;
            countdownTimerText.SetText("FIELD READY!");
        }

        if (startMatchButton != null)
        {
            startMatchButton.gameObject.SetActive(true);
            startMatchButton.interactable = true;
        }
    }

    private IEnumerator CycleFactsRoutine()
    {
        if (gameFacts == null || gameFacts.Length == 0 || gameFactText == null)
            yield break;

        int factIndex = UnityEngine.Random.Range(0, gameFacts.Length);

        while (setupStatusPanel.activeInHierarchy && (startMatchButton == null || !startMatchButton.gameObject.activeSelf))
        {
            gameFactText.SetText(gameFacts[factIndex]);
            factIndex = (factIndex + 1) % gameFacts.Length;

            yield return new WaitForSecondsRealtime(tipCycleInterval);
        }
    }

    private void OnStartMatchClicked()
    {
        ResetTimeScale();
        matchStarted = true;
        isSettling = false;

        if (scoreManager != null)
        {
            scoreManager.ResetScore();
        }

        if (setupStatusPanel != null) setupStatusPanel.SetActive(false);
        if (matchHudPanel != null) matchHudPanel.SetActive(true);
        if (resultsPanel != null) resultsPanel.SetActive(false);
        if (controlsPanel != null) controlsPanel.SetActive(false);

        SetTargetScriptsActive(true);
        if (matchTimer != null) matchTimer.StartTimer();

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void HandleMatchEnded()
    {
        matchStarted = false;
        isSettling = true;

        SetTargetScriptsActive(false);
        StartCoroutine(SettlingPeriodRoutine());
    }

    private IEnumerator SettlingPeriodRoutine()
    {
        float timer = settlingDuration;

        while (timer > 0f)
        {
            if (settlingNoticeText != null)
            {
                settlingNoticeText.gameObject.SetActive(true);
                settlingNoticeText.SetText("SETTLING FIELD: {0:F1}s", timer);
            }

            timer -= Time.deltaTime;
            yield return null;
        }

        if (settlingNoticeText != null)
        {
            settlingNoticeText.gameObject.SetActive(false);
        }

        isSettling = false;
        EvaluateEndgameResults();
    }

    private void EvaluateEndgameResults()
    {
        string zoneTitle = "No Climb";
        float finalMultiplier = 0f;

        if (climbDetector != null)
        {
            finalMultiplier = climbDetector.EvaluateClimbMultiplier(out zoneTitle);
        }

        if (scoreManager != null)
        {
            scoreManager.SetClimbMultiplier(finalMultiplier);

            int compBalls = scoreManager.CompressionBalls;
            int extBalls = scoreManager.ExtinguisherBalls;
            int multipliedComp = Mathf.CeilToInt(compBalls * (1f + finalMultiplier));
            int total = scoreManager.GetTotalScore();

            if (resultsFinalScoreText != null)
            {
                resultsFinalScoreText.SetText("{0}", total);
            }

            if (resultsBreakdownText != null)
            {
                resultsBreakdownText.text = string.Format(
                    "<b>Suppression Unit:</b> {0} Balls (Base: {0} pts)\n" +
                    "<b>Climb Status:</b> {1} (+{2:F2}x)\n" +
                    "<b>Multiplied Suppression:</b> {3} pts\n" +
                    "<b>Extinguisher (Global):</b> {4} pts\n\n" +
                    "<b>Total Alliance Score:</b> {5} PTS",
                    compBalls, zoneTitle, finalMultiplier, multipliedComp, extBalls, total
                );
            }
        }

        if (matchHudPanel != null) matchHudPanel.SetActive(false);
        if (resultsPanel != null) resultsPanel.SetActive(true);

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    #endregion

    #region Pause & Sub-Panels

    public void TogglePause()
    {
        if (!matchStarted || isSettling) return;

        if (isPaused) UnpauseGame();
        else PauseGame();
    }

    public void PauseGame()
    {
        if (!matchStarted || isPaused || isSettling) return;

        isPaused = true;
        prePauseTimeScale = Time.timeScale;

        Time.timeScale = 0f;
        Time.fixedDeltaTime = 0f;
        AudioListener.pause = true;

        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(true);
        if (controlsPanel != null) controlsPanel.SetActive(false);

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void UnpauseGame()
    {
        if (!isPaused) return;

        isPaused = false;
        SetTimeScale(prePauseTimeScale);
        AudioListener.pause = false;

        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        if (controlsPanel != null) controlsPanel.SetActive(false);

        if (matchStarted && !isSettling)
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
        else
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }

    public void OpenControlsSubPanel()
    {
        if (controlsPanel != null) controlsPanel.SetActive(true);
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
    }

    public void CloseControlsSubPanel()
    {
        if (controlsPanel != null) controlsPanel.SetActive(false);
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(true);
    }

    public void ReloadCurrentScene()
    {
        StopAllCoroutines();
        Time.timeScale = 1.0f;
        Time.fixedDeltaTime = originalFixedDeltaTime;
        AudioListener.pause = false;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    #endregion

    #region Helpers

    private void SetTimeScale(float scale)
    {
        Time.timeScale = scale;
        Time.fixedDeltaTime = originalFixedDeltaTime * scale;
    }

    private void ResetTimeScale()
    {
        Time.timeScale = 1.0f;
        Time.fixedDeltaTime = originalFixedDeltaTime;
    }

    private void SetTargetScriptsActive(bool state)
    {
        if (objectsToControl == null) return;

        for (int i = 0; i < objectsToControl.Length; i++)
        {
            GameObject target = objectsToControl[i];
            if (target == null) continue;

            MonoBehaviour[] scripts = includeChildren
                ? target.GetComponentsInChildren<MonoBehaviour>(true)
                : target.GetComponents<MonoBehaviour>();

            for (int j = 0; j < scripts.Length; j++)
            {
                MonoBehaviour script = scripts[j];
                if (script == null || script == this) continue;
                script.enabled = state;
            }
        }
    }

    #endregion
}