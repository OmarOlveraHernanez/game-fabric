using System;
using System.Collections.Generic;
using Tacos.Logic;
using Tacos.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Tacos
{
    /// <summary>
    /// Arrastrar (lo que los jugadores intentaban hacer): la tortilla al domo y del domo al plato, la carne y las cazuelitas
    /// al plato, el plato al cliente o al bote, la carne cruda a su cazo. Tocar sigue funcionando igual.
    /// Todo termina en los mismos Command de siempre (la logica no cambia).
    /// </summary>
    public sealed partial class KitchenScene
    {
        sealed class DragSpec
        {
            public Func<Sprite> Ghost;              // null = ahora no hay nada que arrastrar
            public float Size;                       // pixeles de la imagen
            public Func<List<RectTransform>> Targets;
            public Action<RectTransform> Drop;
        }

        RectTransform _dragLayer, _glowLayer, _domeDrop, _rawHot;
        Image _ghost;
        readonly List<Image> _dropRings = new List<Image>();
        DragSpec _dragging;
        List<RectTransform> _dragTargets;
        RectTransform _hover;

        void BuildDrag()
        {
            _domeDrop = _stage.Img("DomoSoltar", null, new Color(1f, 1f, 1f, 0f), L.ComalDrop).rectTransform;
            _dragLayer = UiKit.Stretch(UiKit.NewRect("Arrastre", _stage.Root));
            // El brillo de donde vas a soltar va DETRAS de los platos y clientes (justo encima del fondo y los videos)
            _glowLayer = UiKit.Stretch(UiKit.NewRect("BrilloSoltar", _stage.Root));
            _glowLayer.SetSiblingIndex(_messLayer.GetSiblingIndex() + 1);
            for (int i = 0; i < 4; i++)
            {
                var ring = UiKit.NewRect("Soltar " + i, _glowLayer).gameObject.AddComponent<Image>();
                ring.sprite = UiKit.GlowSprite; // brillo suave, no aro
                ring.raycastTarget = false;
                ring.enabled = false;
                _dropRings.Add(ring);
            }
            _ghost = UiKit.NewRect("LoQueArrastras", _dragLayer).gameObject.AddComponent<Image>();
            _ghost.raycastTarget = false;
            _ghost.preserveAspect = true;
            _ghost.enabled = false;
            var g = _ghost.rectTransform;
            g.anchorMin = g.anchorMax = g.pivot = new Vector2(0.5f, 0.5f);

            // Bases: de su pila al domo o al comal (tortillas; o sopes, gorditas y quesadillas)
            foreach (var (b, hot) in _stacks)
            {
                var bb = b;
                Draggable(hot, new DragSpec
                {
                    Ghost = () => _sim != null && _sim.Level.NeedsTortilla ? (bb == Base.Tortilla ? TacosArt.Get("ing_tortillas") : TacosArt.Base(bb, false)) : null,
                    Size = bb == Base.Tortilla ? 100f : 110f,
                    Targets = () => One(_domeDrop), Drop = t => _do(Command.AddTortilla(bb))
                });
            }
            // Del domo al plato: ver MakeTortillaDraggable (los lugares del domo se crean en cada mision)
            // Carne: del trompo o del cazo al plato. Vacio: se rellena tocando
            Draggable(_trompoHot, MeatSpec(Meat.Pastor));
            foreach (var b in _bowls) if (b.Hot != null) Draggable(b.Hot, MeatSpec(b.Meat));
            // Carne cruda: de la charola al cazo que quieras rellenar
            Draggable(_rawHot, new DragSpec
            {
                Ghost = () => TacosArt.Get("ing_asada"), Size = 80f,
                Targets = () => { var l = new List<RectTransform>(); foreach (var b in _bowls) if (b.Hot.gameObject.activeInHierarchy) l.Add(b.Hot); return l; },
                Drop = t => { foreach (var b in _bowls) if (b.Hot == t) _do(Command.RefillMeat(b.Meat)); }
            });
            // Cazuelitas al plato (solo agrega; si ya la tiene no se la quita)
            foreach (var kv in _extraHot)
            {
                var e = kv.Key;
                Draggable(kv.Value, new DragSpec
                {
                    Ghost = () => TacosArt.Extra(e), Size = 70f, Targets = PlateTargets,
                    Drop = t =>
                    {
                        int p = PlateIndex(t);
                        if (p < 0 || _sim == null) return;
                        _do(Command.SelectPlate(p));
                        if (!Extras.Has(_sim.Plates[p].Extras, e)) _do(Command.ToggleExtra(e));
                    }
                });
            }
            // Platos: al cliente para entregar o al bote para tirar
            for (int i = 0; i < _plates.Length; i++)
            {
                int p = i;
                Draggable(_plates[i].Hot, new DragSpec
                {
                    Ghost = () => _sim != null && p < _sim.Plates.Length && _sim.Plates[p].HasMeat ? TacosArt.Get("ing_taco") : null, Size = 110f,
                    Targets = () => { var l = new List<RectTransform>(); foreach (var c in _customers) if (c.Hot.gameObject.activeInHierarchy) l.Add(c.Hot); l.Add(_trashHot); return l; },
                    Drop = t =>
                    {
                        _do(Command.SelectPlate(p));
                        if (t == _trashHot) { _do(Command.TrashPlate(p)); return; }
                        for (int c = 0; c < _customers.Length; c++) if (_customers[c].Hot == t) _do(Command.Deliver(c));
                    }
                });
            }
        }

        /// <summary>Las tortillas del domo se crean por mision (2 o 4 lugares): se vuelven arrastrables al crearse.</summary>
        void MakeTortillaDraggable(TortillaView v, int slot)
        {
            Draggable(v.Hot, new DragSpec
            {
                Ghost = () =>
                {
                    var t = _sim != null && slot < _sim.Comal.Length ? _sim.Comal[slot] : null;
                    if (t == null) return null;
                    var st = _sim.TortillaState(t);
                    if (st == CookState.Flip) return null; // primero se voltea (tocandola)
                    if (t.Base == Base.Tortilla) return TacosArt.Get("ing_tortilla");
                    return st == CookState.Burned ? TacosArt.Get("base_quemada") : TacosArt.Base(t.Base, t.Flipped);
                },
                Size = 100f, Targets = PlateTargets,
                Drop = t =>
                {
                    int p = PlateIndex(t);
                    if (p < 0) return;
                    _do(Command.SelectPlate(p));
                    _do(Command.TakeTortilla(slot));
                }
            });
        }

        DragSpec MeatSpec(Meat m) => new DragSpec
        {
            Ghost = () =>
            {
                var pot = _sim?.Pot(m);
                return pot != null && pot.Portions > 0 && !pot.IsRefilling ? TacosArt.Meat(m) : null;
            },
            Size = 84f, Targets = PlateTargets,
            Drop = t =>
            {
                int p = PlateIndex(t);
                if (p < 0) return;
                _do(Command.SelectPlate(p));
                _do(Command.ServeMeat(m));
            }
        };

        List<RectTransform> PlateTargets()
        {
            var l = new List<RectTransform>();
            foreach (var p in _plates) if (p.Hot.gameObject.activeInHierarchy) l.Add(p.Hot);
            return l;
        }

        int PlateIndex(RectTransform t)
        {
            for (int i = 0; i < _plates.Length; i++) if (_plates[i].Hot == t) return i;
            return -1;
        }

        static List<RectTransform> One(RectTransform t) => new List<RectTransform> { t };

        void Draggable(RectTransform hot, DragSpec spec)
        {
            if (hot == null) return;
            var src = hot.gameObject.GetComponent<DragSource>() ?? hot.gameObject.AddComponent<DragSource>();
            src.Begin = () =>
            {
                if (_sim == null || _sim.IsFinished) return false;
                var sprite = spec.Ghost();
                if (sprite == null) return false;
                _dragging = spec;
                _dragTargets = spec.Targets();
                _ghost.sprite = sprite;
                _ghost.enabled = true;
                float k = _stage.Root.rect.width / W;
                _ghost.rectTransform.sizeDelta = new Vector2(spec.Size * k, spec.Size * k);
                _dragLayer.SetAsLastSibling();
                return true;
            };
            src.Move = MoveDrag;
            src.Cancel = EndDrag;
            src.Drop = screen =>
            {
                MoveDrag(screen);
                var target = _hover;
                var spec2 = _dragging;
                EndDrag();
                if (target != null) { UiKit.Clicked?.Invoke(); spec2.Drop(target); }
            };
        }

        void MoveDrag(Vector2 screen)
        {
            if (_dragging == null) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_stage.Root, screen, null, out var local);
            float k = _stage.Root.rect.width / W;
            // un poco arriba del dedo para que se vea lo que llevas
            _ghost.rectTransform.anchoredPosition = local + new Vector2(0f, _dragging.Size * 0.45f * k);

            _hover = null;
            float best = float.MaxValue;
            foreach (var t in _dragTargets)
            {
                if (t == null || !t.gameObject.activeInHierarchy) continue;
                if (!RectTransformUtility.RectangleContainsScreenPoint(t, screen, null)) continue;
                var c = RectTransformUtility.WorldToScreenPoint(null, t.TransformPoint(t.rect.center));
                float d = (c - screen).sqrMagnitude;
                if (d < best) { best = d; _hover = t; }
            }
            // Solo se ilumina el lugar que esta debajo del dedo (los aros en todos parecian pistas del tutorial)
            for (int i = 0; i < _dropRings.Count; i++)
            {
                var ring = _dropRings[i];
                var t = i < _dragTargets.Count ? _dragTargets[i] : null;
                ring.enabled = t != null && t == _hover && t.gameObject.activeInHierarchy;
                if (!ring.enabled) continue;
                var rt = ring.rectTransform;
                rt.anchorMin = t.anchorMin; rt.anchorMax = t.anchorMax;
                rt.SetParent(t.parent == _stage.Root ? _glowLayer : t.parent, false);
                float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 6f);
                float grow = 26f + 8f * pulse;
                rt.offsetMin = new Vector2(-grow, -grow); rt.offsetMax = new Vector2(grow, grow);
                ring.color = new Color(1f, 0.92f, 0.6f, 0.45f + 0.2f * pulse); // calido, como la luz del puesto
            }
            float s = _hover != null ? 1.12f : 1f;
            _ghost.rectTransform.localScale = new Vector3(s, s, 1f);
        }

        void EndDrag()
        {
            _dragging = null;
            _hover = null;
            _ghost.enabled = false;
            foreach (var r in _dropRings) { r.enabled = false; r.rectTransform.SetParent(_glowLayer, false); }
        }

        /// <summary>De donde a donde se arrastra para cumplir la pista (lo usa la manita del tutorial). null = es un toque.</summary>
        public (RectTransform from, RectTransform to)? HintDrag(Hint h)
        {
            if (_sim == null) return null;
            int sel = Mathf.Clamp(_sim.SelectedPlate, 0, _plates.Length - 1);
            RectTransform plate = _plates[h.Plate >= 0 && h.Plate < _plates.Length ? h.Plate : sel].Hot;
            switch (h.Kind)
            {
                case HintKind.AddTortilla: return (StackFor(h.Command.HasValue ? (Base)h.Command.Value.Arg : Base.None), _domeDrop);
                case HintKind.TakeTortilla: return h.Slot >= 0 && h.Slot < _tortillas.Count ? (_tortillas[h.Slot].Hot, plate) : ((RectTransform, RectTransform)?)null;
                case HintKind.ServeMeat:
                {
                    var from = HintTarget(h);
                    return from != null ? (from, plate) : ((RectTransform, RectTransform)?)null;
                }
                case HintKind.AddExtra: return _extraHot.TryGetValue(h.Extra, out var e) ? (e, _plates[sel].Hot) : ((RectTransform, RectTransform)?)null;
                case HintKind.Deliver:
                {
                    if (h.Customer < 0 || _sim.Customers[h.Customer] == null) return null;
                    int p = sel;
                    for (int i = 0; i < _sim.Plates.Length; i++) if (_sim.Plates[i].Matches(_sim.Customers[h.Customer].Order)) { p = i; break; }
                    return (_plates[p].Hot, _customers[h.Customer].Hot);
                }
                case HintKind.TrashPlate: return (plate, _trashHot);
                default: return null;
            }
        }
    }
}
