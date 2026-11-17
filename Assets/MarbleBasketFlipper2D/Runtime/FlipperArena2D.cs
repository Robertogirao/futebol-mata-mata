using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace MarbleBasketFlipper2D
{
    public enum LavaRule2D { VoltaAoFunil, EliminadaNaPartida }

    /// <summary>Independent 2D funnel/flipper match with two scoring hoops.</summary>
    [DefaultExecutionOrder(-500)]
    public sealed class FlipperArena2D : MonoBehaviour
    {
        [Header("Partida")]
        [Range(1, 6)] public int bolasPorTime = 5;
        [Min(10)] public float duracaoSegundos = 120;
        public string nomeVermelho = "VERMELHO";
        public string nomeAzul = "AZUL";
        [Tooltip("Regra para uma bolinha que erra e cai na lava ou sai da arena. Uma cesta volta ao funil em ambos os modos.")]
        public LavaRule2D regraDaLava = LavaRule2D.VoltaAoFunil;
        [Tooltip("Variação adicional do impulso, igual para as duas equipes.")]
        [Range(0, 1)] public float variacaoArremesso = .8f;
        [Tooltip("Velocidade média dos movimentos completos de ida e volta da alavanca, por segundo.")]
        [Range(.5f, 2f)] public float velocidadeDaAlavanca = 1.5f;
        [Tooltip("Velocidade com que o ponto de nascimento se move para os lados dentro do funil.")]
        [Range(.2f, 3f)] public float velocidadeDoNascimento = 1.1f;
        [Tooltip("Velocidade da barra que sobe e desce diante das cestas, em ciclos por segundo.")]
        [Range(.15f, .9f)] public float velocidadeDaBarreira = .35f;

        public int RedScore { get; private set; }
        public int BlueScore { get; private set; }
        public float RemainingSeconds { get; private set; }
        public bool Finished { get; private set; }
        public string RedName => string.IsNullOrWhiteSpace(nomeVermelho) ? "VERMELHO" : nomeVermelho;
        public string BlueName => string.IsNullOrWhiteSpace(nomeAzul) ? "AZUL" : nomeAzul;
        public Camera Camera { get; private set; }

        private enum Phase { InChute, Flight, Scored, Burning, Returning, Eliminated }
        private sealed class Marble
        {
            public int team, scoredHoop;
            public Rigidbody2D body;
            public CircleCollider2D collider;
            public GameObject visual;
            public Phase phase;
            public float age, stagnantTime, floorAge;
            public bool ignoringExit, scoredBeforeBurn;
            public Vector2 previous, progressAnchor, burnOrigin;
            public SpriteRenderer ballSprite, reflection, fireOuter, fireCore;
        }

        private const float BallRadius = .17f;
        private const float Gravity = 9.81f;
        private const float RestAngle = (float)FlipperMotion2D.MinimumAngle;
        private const float LowY = FlipperRules2D.LowY, LowLeft = FlipperRules2D.LowLeft, LowRight = FlipperRules2D.LowRight;
        private const float HighY = FlipperRules2D.HighY, HighLeft = FlipperRules2D.HighLeft, HighRight = FlipperRules2D.HighRight;
        private static readonly Vector2 Pivot = new Vector2(-6.00f, -3.08f);
        private static readonly Color Red = new Color(.98f, .035f, .06f);
        private static readonly Color Blue = new Color(.04f, .30f, 1f);
        private static readonly Color Steel = new Color(.55f, .68f, .86f);
        private static readonly Color Neon = new Color(.18f, .78f, 1f);
        private static readonly Color Dark = new Color(.035f, .055f, .10f);
        private readonly List<Marble> balls = new List<Marble>();
        private readonly Dictionary<Rigidbody2D, Marble> ballLookup = new Dictionary<Rigidbody2D, Marble>();
        private readonly Dictionary<Marble, float> pendingImpacts = new Dictionary<Marble, float>();
        private readonly List<UnityEngine.Object> assets = new List<UnityEngine.Object>();
        private readonly System.Random random = new System.Random();
        private Transform world;
        private FlipperArt2D art;
        private Rigidbody2D flipper, obstacle;
        private Collider2D flipperCollider;
        private PhysicsMaterial2D tracks, ballMaterial;
        private float elapsed, flipperAngle = RestAngle, flipperAngularVelocity;
        private RenderPipelineAsset oldGraphics, oldQuality;
        private bool pipelineAcquired;

        private void Awake()
        {
            bolasPorTime = Mathf.Clamp(bolasPorTime, 1, 6);
            duracaoSegundos = Mathf.Max(10, duracaoSegundos);
            variacaoArremesso = Mathf.Clamp01(variacaoArremesso);
            velocidadeDoNascimento = velocidadeDoNascimento <= 0 ? 1.1f : Mathf.Clamp(velocidadeDoNascimento, .2f, 3f);
            velocidadeDaAlavanca = velocidadeDaAlavanca <= 0 ? 1.5f : Mathf.Clamp(velocidadeDaAlavanca, .5f, 2f);
            velocidadeDaBarreira = velocidadeDaBarreira <= 0 ? .35f : Mathf.Clamp(velocidadeDaBarreira, .15f, .9f);
            RemainingSeconds = duracaoSegundos;
            oldGraphics = GraphicsSettings.defaultRenderPipeline; oldQuality = QualitySettings.renderPipeline;
            pipelineAcquired = true; QualitySettings.renderPipeline = null; GraphicsSettings.defaultRenderPipeline = null;
            try { Build(); }
            catch (Exception error)
            {
                Debug.LogException(error, this); RestorePipeline();
                if (world != null) world.gameObject.SetActive(false);
                enabled = false;
            }
        }
        private void Build()
        {
            world = Node("Arena de duas cestas gerada", transform);
            art = new FlipperArt2D(world); BuildCamera(); art.Background();
            tracks = Material("Trilhos do funil", .025f, .25f);
            ballMaterial = Material("Bolinha do funil", .035f, .40f);
            BuildChute(); BuildFlipper(); BuildFloor(); BuildObstacle();
            BuildBasket("Cesta de 2", LowLeft, LowRight, LowY);
            BuildBasket("Cesta de 3", HighLeft, HighRight, HighY);
            for (int i = 0; i < bolasPorTime; i++) { NewBall(-1, i); NewBall(1, i); }
            var positions = SpawnField2D.InitialPositions(balls.Count, BallRadius, random);
            for (int i = 0; i < balls.Count; i++) Spawn(balls[i], positions[i]);
            var hud = gameObject.AddComponent<FlipperHud2D>(); hud.arena = this; hud.panel = art.PanelTexture;
        }
        private static Transform Node(string name, Transform parent)
        {
            var t = new GameObject(name).transform; t.SetParent(parent, false); return t;
        }
        private PhysicsMaterial2D Material(string name, float friction, float bounce)
        {
            var m = new PhysicsMaterial2D(name) { friction = friction, bounciness = bounce }; assets.Add(m); return m;
        }
        private void BuildCamera()
        {
            var go = Node("Câmera 2D", world).gameObject; go.tag = "MainCamera";
            go.transform.position = new Vector3(0, 0, -10);
            Camera = go.AddComponent<Camera>(); Camera.orthographic = true; Camera.orthographicSize = 4.5f;
            Camera.clearFlags = CameraClearFlags.SolidColor; Camera.backgroundColor = new Color(.015f, .02f, .06f);
            Camera.nearClipPlane = .1f; Camera.farClipPlane = 30; Camera.allowHDR = false; Camera.allowMSAA = true;
        }
        private void LateUpdate()
        {
            if (Camera == null || Screen.height <= 0) return;
            float window = (float)Screen.width / Screen.height, desired = 16f / 9;
            Camera.rect = window > desired ? new Rect((1 - desired / window) * .5f, 0, desired / window, 1)
                : new Rect(0, (1 - window / desired) * .5f, 1, window / desired);
            Camera.aspect = desired;
        }
        private void BuildChute()
        {
            Vector2 leftTop = new Vector2((float)SpawnField2D.LeftTop, (float)SpawnField2D.TopY), rightTop = new Vector2((float)SpawnField2D.RightTop, (float)SpawnField2D.TopY);
            Vector2 leftNeck = new Vector2((float)SpawnField2D.LeftNeck, (float)SpawnField2D.NeckY), rightNeck = new Vector2((float)SpawnField2D.RightNeck, (float)SpawnField2D.NeckY);
            Rail("Funil esquerdo", leftTop, leftNeck, .14f);
            Rail("Funil direito", rightTop, rightNeck, .14f);
            Rail("Parede esquerda", leftNeck, new Vector2(-6.24f, -2.65f), .10f);
            // The vertical right wall is absent, leaving the requested open side.
            Rail("Curva inferior 1", new Vector2(-6.24f, -2.65f), new Vector2(-6.13f, -2.90f), .10f);
            Rail("Curva inferior 2", new Vector2(-6.13f, -2.90f), new Vector2(-6.00f, -3.12f), .10f);
        }
        private void BuildFlipper()
        {
            var pivot = Node("Lançador móvel", world); pivot.localPosition = Pivot;
            flipper = pivot.gameObject.AddComponent<Rigidbody2D>(); flipper.bodyType = RigidbodyType2D.Kinematic;
            flipper.rotation = RestAngle;
            // The open funnel leaves more balls to the right, so extend the active paddle to catch them.
            var blade = art.Box("Alavanca móvel", new Vector2(1.20f, 0), new Vector2(2.4f, .24f), Steel, 3, pivot);
            var collider = blade.gameObject.AddComponent<BoxCollider2D>(); collider.size = new Vector2(.94f, .80f); collider.sharedMaterial = tracks;
            flipperCollider = collider;
            var relay = pivot.gameObject.AddComponent<FlipperImpactRelay2D>(); relay.arena = this;
            art.Disk("Eixo · borda", Vector2.zero, .32f, Neon, 5, pivot);
            art.Disk("Eixo do lançador", Vector2.zero, .26f, Steel, 6, pivot);
            art.Disk("Centro do eixo", Vector2.zero, .14f, Dark, 7, pivot);
            art.Disk("Eixo · parafuso", Vector2.zero, .065f, new Color(.88f, .95f, 1f), 8, pivot);
        }
        private void BuildFloor()
        {
            // Visible floor and lava are already part of the background artwork.
            // Add physical rails only to the three solid stretches.
            float y = (float)ArenaSurface2D.FloorY - .05f;
            PhysicalRail("Chão esquerdo", new Vector2((float)ArenaSurface2D.LeftEdge, y), new Vector2((float)ArenaSurface2D.LavaOneLeft, y), .10f, tracks);
            PhysicalRail("Chão central", new Vector2((float)ArenaSurface2D.LavaOneRight, y), new Vector2((float)ArenaSurface2D.LavaTwoLeft, y), .10f, tracks);
            PhysicalRail("Chão direito", new Vector2((float)ArenaSurface2D.LavaTwoRight, y), new Vector2((float)ArenaSurface2D.RightEdge, y), .10f, tracks);
        }
        private void BuildObstacle()
        {
            var t = Node("Barreira móvel", world);
            t.localPosition = new Vector2((float)ArenaSurface2D.ObstacleX, (float)ArenaSurface2D.ObstacleY(0, velocidadeDaBarreira));
            obstacle = t.gameObject.AddComponent<Rigidbody2D>(); obstacle.bodyType = RigidbodyType2D.Kinematic;
            var frame = art.Box("Barra · moldura", Vector2.zero, new Vector2(.24f, 1.05f), Steel, 4, t);
            var collider = frame.gameObject.AddComponent<BoxCollider2D>(); collider.size = new Vector2(.90f, .96f); collider.sharedMaterial = tracks;
            art.Box("Barra · neon", Vector2.zero, new Vector2(.14f, .95f), Neon, 5, t);
            art.Box("Barra · brilho", Vector2.zero, new Vector2(.055f, .85f), Color.white, 6, t);
        }
        private void BuildBasket(string name, float left, float right, float y)
        {
            float postX = right + .17f;
            BasketStructure(name, postX, right, y);
            PhysicalRail(name + " · tabela física", new Vector2(postX - .05f, y), new Vector2(postX - .05f, y + 1.05f), .08f, tracks);
            CirclePost(name + " · borda esquerda", new Vector2(left, y));
            CirclePost(name + " · borda direita", new Vector2(right, y));
            if (!art.BasketHoop(name + " · aro e rede ilustrados", left, right, y))
            {
                // Fallback keeps the basket visible if a texture was not imported.
                art.Line(name + " · aro externo", new Vector2(left, y), new Vector2(right, y), .13f, new Color(1f, .10f, .12f), 5);
                art.Line(name + " · brilho", new Vector2(left, y + .02f), new Vector2(right, y + .02f), .035f, Color.white, 6);
                float cx = (left + right) * .5f;
                for (int i = 0; i <= 6; i++)
                {
                    float top = Mathf.Lerp(left, right, i / 6f), bottom = Mathf.Lerp(cx - .33f, cx + .33f, i / 6f);
                    art.Line(name + " · rede", new Vector2(top, y - .02f), new Vector2(bottom, y - .62f), .017f, Color.white, 5);
                }
                art.Line(name + " · fundo da rede", new Vector2(cx - .33f, y - .62f), new Vector2(cx + .33f, y - .62f), .025f, Color.white, 5);
            }
        }
        private void BasketStructure(string name, float postX, float right, float y)
        {
            // Decorative pieces sit behind the balls; the physical backboard remains unchanged.
            float poleX = postX + .12f, footY = -3.72f;
            Vector2 bottom = new Vector2(poleX, footY), top = new Vector2(poleX, y + .13f);
            if (!art.GuideRail(name + " · poste metálico", bottom, top, .25f, -4))
                art.Line(name + " · poste", bottom, top, .19f, Steel, -4);
            art.Box(name + " · base sombra", new Vector2(poleX, footY), new Vector2(.66f, .18f), Dark, -3);
            art.Box(name + " · base aço", new Vector2(poleX, footY + .025f), new Vector2(.56f, .105f), Steel, -2);
            art.Line(name + " · base luz", new Vector2(poleX - .22f, footY + .055f), new Vector2(poleX + .22f, footY + .055f), .025f, Neon, -1);
            art.Disk(name + " · base parafuso esquerdo", new Vector2(poleX - .23f, footY + .01f), .045f, Dark, 0);
            art.Disk(name + " · base parafuso direito", new Vector2(poleX + .23f, footY + .01f), .045f, Dark, 0);

            float boardX = postX + .11f;
            art.Box(name + " · moldura da tabela", new Vector2(boardX, y + .55f), new Vector2(.32f, 1.18f), Steel, 0);
            art.Box(name + " · luz da tabela", new Vector2(boardX, y + .55f), new Vector2(.26f, 1.11f), Neon, 1);
            art.Box(name + " · face da tabela", new Vector2(boardX, y + .55f), new Vector2(.19f, 1.04f), new Color(.88f, .94f, 1f), 2);
            art.Box(name + " · faixa da tabela", new Vector2(boardX + .025f, y + .55f), new Vector2(.045f, .70f), new Color(.80f, .04f, .09f), 3);
            art.Line(name + " · reflexo da tabela", new Vector2(boardX - .045f, y + .10f), new Vector2(boardX - .045f, y + 1.00f), .025f, Color.white, 3);
            art.Disk(name + " · fixação superior", new Vector2(boardX, y + 1.05f), .055f, Dark, 4);
            art.Disk(name + " · fixação inferior", new Vector2(boardX, y + .05f), .055f, Dark, 4);
            art.Line(name + " · encaixe do aro", new Vector2(right, y), new Vector2(postX + .02f, y), .10f, Steel, 4);
        }
        private void CirclePost(string name, Vector2 position)
        {
            var t = Node(name, world); t.localPosition = position;
            var c = t.gameObject.AddComponent<CircleCollider2D>(); c.radius = .06f; c.sharedMaterial = tracks;
        }
        private void VisualRail(string name, Vector2 a, Vector2 b, float thickness)
        {
            if (art.GuideRail(name + " · trilho ilustrado", a, b, thickness + .12f, 2)) return;
            art.Line(name + " · sombra", a, b, thickness + .18f, Dark, 0);
            art.Line(name + " · neon", a, b, thickness + .06f, new Color(.04f, .50f, 1f), 1);
            art.Line(name + " · aço", a, b, thickness, Steel, 2);
            art.Line(name + " · reflexo", a, b, .025f, Color.white, 3);
        }
        private Collider2D Rail(string name, Vector2 a, Vector2 b, float thickness)
        {
            VisualRail(name, a, b, thickness); return PhysicalRail(name, a, b, thickness, tracks);
        }
        private Collider2D PhysicalRail(string name, Vector2 a, Vector2 b, float thickness, PhysicsMaterial2D material)
        {
            var t = Node(name + " · colisão", world); Vector2 d = b - a;
            t.localPosition = (a + b) * .5f; t.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
            var c = t.gameObject.AddComponent<BoxCollider2D>(); c.size = new Vector2(d.magnitude, thickness); c.sharedMaterial = material; return c;
        }
        private void NewBall(int team, int index)
        {
            var t = Node((team < 0 ? "Vermelho " : "Azul ") + (index + 1), world);
            var body = t.gameObject.AddComponent<Rigidbody2D>(); body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0; body.collisionDetectionMode = CollisionDetectionMode2D.Continuous; body.interpolation = RigidbodyInterpolation2D.Interpolate;
            var collider = t.gameObject.AddComponent<CircleCollider2D>(); collider.radius = BallRadius; collider.sharedMaterial = ballMaterial; collider.enabled = false;
            var ballSprite = art.Marble("Bolinha", Vector2.zero, BallRadius * 2, team < 0 ? Red : Blue, 10, t);
            var reflection = art.Disk("Reflexo", new Vector2(-.06f, .065f), .055f, new Color(1, 1, 1, .9f), 11, t);
            var fireOuter = art.Disk("Chama externa", new Vector2(0, .11f), .35f, new Color(1, .24f, .025f, .85f), 12, t);
            var fireCore = art.Disk("Chama interna", new Vector2(0, .11f), .20f, new Color(1, .84f, .18f, .95f), 13, t);
            fireOuter.enabled = false; fireCore.enabled = false;
            var ball = new Marble { team = team, body = body, collider = collider, visual = t.gameObject,
                ballSprite = ballSprite, reflection = reflection, fireOuter = fireOuter, fireCore = fireCore, phase = Phase.Returning };
            balls.Add(ball); ballLookup.Add(body, ball);
        }
        private static void SetVisible(Marble b, bool value)
        {
            foreach (var sprite in b.visual.GetComponentsInChildren<SpriteRenderer>(true)) sprite.enabled = value;
        }

        private void FixedUpdate()
        {
            if (world == null || Finished) return;
            float dt = Time.fixedDeltaTime; elapsed += dt; RemainingSeconds = Mathf.Max(0, duracaoSegundos - elapsed);
            if (RemainingSeconds <= 0) { Finish(); return; }
            ApplyPendingImpacts();
            float nextAngle = (float)FlipperMotion2D.Angle(elapsed, velocidadeDaAlavanca);
            flipperAngularVelocity = (nextAngle - flipperAngle) / dt;
            flipperAngle = nextAngle; flipper.MoveRotation(flipperAngle);
            obstacle.MovePosition(new Vector2((float)ArenaSurface2D.ObstacleX, (float)ArenaSurface2D.ObstacleY(elapsed, velocidadeDaBarreira)));
            foreach (var b in balls) Advance(b, dt);
            if (regraDaLava == LavaRule2D.EliminadaNaPartida && AllEliminated()) Finish();
        }
        private bool AllEliminated()
        {
            foreach (var b in balls) if (b.phase != Phase.Eliminated) return false;
            return true;
        }
        private void Advance(Marble b, float dt)
        {
            b.age += dt;
            if (b.phase == Phase.Eliminated) return;
            if (b.phase == Phase.Returning)
            {
                TryRespawn(b);
                return;
            }
            if (b.phase == Phase.Burning) { AnimateBurn(b); return; }
            Vector2 current = b.body.position;
            if (b.ignoringExit && b.age >= .18f) IgnoreExit(b, false);
            // A ball may leave through the wider opening after another ball's bounce.
            if (b.phase == Phase.InChute && current.x > -4.05f) { b.phase = Phase.Flight; b.age = 0; }
            if (b.phase == Phase.Flight)
            {
                int points = FlipperRules2D.PointsCrossed(P(b.previous), P(current), BallRadius);
                if (points > 0) Score(b, points);
            }
            UpdateBasketLayer(b, current);
            if (ArenaSurface2D.TouchesFloor(current.y, BallRadius))
            {
                float x = SurfaceContactX(b.previous, current);
                if (ArenaSurface2D.IsLava(x) && GetVelocity(b.body).y <= 0) { StartBurn(b); return; }
                if (!ArenaSurface2D.IsLava(x) && b.phase != Phase.InChute && b.floorAge < 0) b.floorAge = 0;
            }
            b.previous = current;
            if (b.floorAge >= 0)
            {
                b.floorAge += dt;
                float remaining = (float)ArenaSurface2D.FloorLifetime - b.floorAge;
                if (remaining <= 0) { RecoverOrEliminate(b, false); return; }
                if (remaining < .5f) SetBallAlpha(b, remaining / .5f);
            }
            if (b.phase == Phase.InChute)
            {
                if ((current - b.progressAnchor).sqrMagnitude > .0064f) { b.progressAnchor = current; b.stagnantTime = 0; }
                else b.stagnantTime += dt;
                // A gentle physical nudge is only used if a ball settles beside the pivot.
                if (b.stagnantTime > 1.2f && current.y < -2.25f)
                {
                    b.body.AddForce(new Vector2(.9f, 1.4f) * b.body.mass, ForceMode2D.Impulse);
                    b.stagnantTime = 0; b.progressAnchor = current;
                }
            }
            bool stuckInChute = b.phase == Phase.InChute && b.stagnantTime > 8f;
            if (current.y < -4.15f || current.x > 7.75f || current.x < -7.8f || current.y > 4.35f ||
                stuckInChute || (b.floorAge < 0 && (b.phase == Phase.Flight || b.phase == Phase.Scored) && b.age > 8f))
            {
                RecoverOrEliminate(b, stuckInChute); return;
            }
            b.body.AddForce(Vector2.down * (Gravity * b.body.mass), ForceMode2D.Force);
        }
        private static float SurfaceContactX(Vector2 previous, Vector2 current)
        {
            float y = (float)ArenaSurface2D.FloorY + BallRadius + .035f;
            if (previous.y > y && current.y < y && previous.y - current.y > .0001f)
                return Mathf.Lerp(previous.x, current.x, (previous.y - y) / (previous.y - current.y));
            return current.x;
        }
        private static void SetBallAlpha(Marble b, float alpha)
        {
            Color color = b.team < 0 ? Red : Blue; color.a = Mathf.Clamp01(alpha); b.ballSprite.color = color;
            b.reflection.color = new Color(1, 1, 1, .9f * Mathf.Clamp01(alpha));
        }
        private static void ResetBallVisual(Marble b)
        {
            SetBallAlpha(b, 1);
            b.ballSprite.sortingOrder = 10; b.reflection.sortingOrder = 11;
            b.fireOuter.enabled = false; b.fireCore.enabled = false;
        }
        private void StartBurn(Marble b)
        {
            b.scoredBeforeBurn = b.phase == Phase.Scored;
            if (b.ignoringExit) IgnoreExit(b, false);
            b.collider.enabled = false;
            SetVelocity(b.body, Vector2.zero); b.body.angularVelocity = 0;
            b.body.bodyType = RigidbodyType2D.Kinematic; b.burnOrigin = b.body.position;
            b.phase = Phase.Burning; b.age = 0;
            b.fireOuter.enabled = true; b.fireCore.enabled = true;
        }
        private void AnimateBurn(Marble b)
        {
            float p = Mathf.Clamp01(b.age / (float)ArenaSurface2D.BurnDuration);
            if (p >= 1) { RecoverOrEliminate(b, false); return; }
            b.body.position = b.burnOrigin + Vector2.up * (.12f * p);
            Color color = Color.Lerp(b.team < 0 ? Red : Blue, new Color(1, .16f, .02f), Mathf.Clamp01(p * 2));
            color.a = 1 - p; b.ballSprite.color = color;
            b.reflection.color = new Color(1, 1, 1, .9f * (1 - p));
            float flicker = 1 + .10f * Mathf.Sin(b.age * 24f);
            b.fireOuter.transform.localPosition = new Vector3(.025f * Mathf.Sin(b.age * 19f), .12f + .08f * p, 0);
            b.fireCore.transform.localPosition = new Vector3(0, .11f + .07f * p, 0);
            b.fireOuter.transform.localScale = new Vector3(.35f * flicker * (1 - .55f * p), .48f * (1 - .45f * p), 1);
            b.fireCore.transform.localScale = new Vector3(.20f * flicker * (1 - .60f * p), .29f * (1 - .50f * p), 1);
            b.fireOuter.color = new Color(1, .24f, .025f, .85f * (1 - p));
            b.fireCore.color = new Color(1, .84f, .18f, .95f * (1 - p));
        }
        private void Spawn(Marble b, Point2D point)
        {
            b.body.position = new Vector2((float)point.x, (float)point.y); b.body.bodyType = RigidbodyType2D.Dynamic;
            SetVelocity(b.body, Vector2.zero); b.body.angularVelocity = 0; b.collider.enabled = true;
            if (b.ignoringExit) IgnoreExit(b, false);
            b.previous = b.body.position; b.progressAnchor = b.previous; b.stagnantTime = 0;
            b.floorAge = -1; b.scoredBeforeBurn = false; b.scoredHoop = 0;
            b.phase = Phase.InChute; b.age = 0; SetVisible(b, true); ResetBallVisual(b);
        }
        private void TryRespawn(Marble b)
        {
            const float minimum = BallRadius * 2f + .02f;
            for (int i = 0; i < 48; i++)
            {
                var candidate = SpawnField2D.Candidate(random, BallRadius, elapsed, velocidadeDoNascimento, i);
                bool free = true;
                foreach (var other in balls)
                {
                    if (other == b || other.phase == Phase.Returning || other.phase == Phase.Eliminated) continue;
                    Vector2 diff = other.body.position - new Vector2((float)candidate.x, (float)candidate.y);
                    if (diff.sqrMagnitude < minimum * minimum) { free = false; break; }
                }
                if (free) { Spawn(b, candidate); return; }
            }
        }
        internal void RecordFlipperContact(Rigidbody2D body)
        {
            // Collision callbacks occur during the physics step. Process each contact once on the next step.
            if (Finished || flipperAngularVelocity <= 5 || body == null) return;
            if (ballLookup.TryGetValue(body, out var b) && b.phase == Phase.InChute && !pendingImpacts.ContainsKey(b))
                pendingImpacts.Add(b, flipperAngle);
        }
        private void ApplyPendingImpacts()
        {
            foreach (var impact in pendingImpacts)
            {
                var b = impact.Key;
                if (b.phase != Phase.InChute || !b.collider.enabled) continue;
                Vector2 incoming = GetVelocity(b.body), position = b.body.position;
                var desired = FlipperMotion2D.AssistedVelocity(P(position), P(incoming), impact.Value, variacaoArremesso,
                    random.NextDouble() * 2 - 1, random.NextDouble() * 2 - 1);
                // The body remains dynamic. The impulse depends on the actual contact and the lever's angle.
                Vector2 change = new Vector2((float)desired.x, (float)desired.y) - incoming;
                IgnoreExit(b, true);
                b.body.AddForce(change * b.body.mass, ForceMode2D.Impulse);
                b.previous = position; b.phase = Phase.Flight; b.age = 0;
            }
            pendingImpacts.Clear();
        }
        private void IgnoreExit(Marble b, bool value)
        {
            if (flipperCollider != null) Physics2D.IgnoreCollision(b.collider, flipperCollider, value);
            b.ignoringExit = value;
        }
        private void Score(Marble b, int points)
        {
            if (b.team < 0) RedScore += points; else BlueScore += points;
            b.scoredHoop = points; b.phase = Phase.Scored; b.age = 0;
        }
        private void UpdateBasketLayer(Marble b, Vector2 position)
        {
            bool behindNet = false;
            if (b.phase == Phase.Scored && (b.scoredHoop == 2 || b.scoredHoop == 3))
            {
                float left = b.scoredHoop == 3 ? HighLeft : LowLeft;
                float right = b.scoredHoop == 3 ? HighRight : LowRight;
                float y = b.scoredHoop == 3 ? HighY : LowY;
                // Keep the ball behind the transparent net only while it overlaps that basket.
                behindNet = position.x >= left - BallRadius && position.x <= right + BallRadius &&
                    position.y >= y - .62f - BallRadius && position.y <= y + BallRadius;
            }
            else if (b.phase == Phase.Flight && GetVelocity(b.body).y < 0)
            {
                // As the ball touches the mouth, place the front of the rim over it.
                behindNet = ApproachingRim(position, LowLeft, LowRight, LowY) ||
                    ApproachingRim(position, HighLeft, HighRight, HighY);
            }
            b.ballSprite.sortingOrder = behindNet ? 3 : 10;
            b.reflection.sortingOrder = behindNet ? 4 : 11;
        }
        private static bool ApproachingRim(Vector2 position, float left, float right, float y)
        {
            return position.x >= left + BallRadius && position.x <= right - BallRadius &&
                position.y >= y && position.y <= y + BallRadius;
        }
        private void RecoverOrEliminate(Marble b, bool stuckInChute)
        {
            bool madeBasket = b.phase == Phase.Scored || (b.phase == Phase.Burning && b.scoredBeforeBurn);
            if (b.ignoringExit) IgnoreExit(b, false);
            b.collider.enabled = false; SetVelocity(b.body, Vector2.zero); b.body.angularVelocity = 0;
            b.body.bodyType = RigidbodyType2D.Kinematic; SetVisible(b, false);
            if (FlipperRules2D.Eliminate(madeBasket, stuckInChute, regraDaLava == LavaRule2D.EliminadaNaPartida)) b.phase = Phase.Eliminated;
            else b.phase = Phase.Returning;
            b.age = 0;
        }
        private void Finish()
        {
            Finished = true;
            foreach (var b in balls) { SetVelocity(b.body, Vector2.zero); b.body.angularVelocity = 0; b.body.simulated = false; }
        }
        private static Point2D P(Vector2 v) => new Point2D(v.x, v.y);
        private static Vector2 GetVelocity(Rigidbody2D body)
        {
#if UNITY_6000_0_OR_NEWER
            return body.linearVelocity;
#else
            return body.velocity;
#endif
        }
        private static void SetVelocity(Rigidbody2D body, Vector2 velocity)
        {
#if UNITY_6000_0_OR_NEWER
            body.linearVelocity = velocity;
#else
            body.velocity = velocity;
#endif
        }
        private void OnDestroy()
        {
            RestorePipeline(); if (art != null) art.Dispose();
            foreach (var asset in assets)
            {
                if (asset == null) continue;
                if (Application.isPlaying) Destroy(asset); else DestroyImmediate(asset);
            }
            assets.Clear();
        }
        private void RestorePipeline()
        {
            if (!pipelineAcquired) return;
            if (QualitySettings.renderPipeline == null) QualitySettings.renderPipeline = oldQuality;
            if (GraphicsSettings.defaultRenderPipeline == null) GraphicsSettings.defaultRenderPipeline = oldGraphics;
            pipelineAcquired = false;
        }
    }
}
