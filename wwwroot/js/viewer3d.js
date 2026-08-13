/*
 * Mekanika 3D geometry viewer
 * ---------------------------
 * Opens a full-screen viewer showing the geometry of a completed calculation.
 *
 * three.js is loaded LAZILY on first use - it is ~600 KB and must not be added
 * to the initial page load, which already carries the Blazor WASM runtime.
 *
 * Adding a new module means writing one entry in BUILDERS. Everything else
 * (scene, lighting, orbit controls, explode, transparency, legend, overlay UI)
 * is shared.
 *
 * A builder receives (THREE, p) where p is the parameter object sent from C#,
 * and returns:
 *   {
 *     title:   string shown bottom-right
 *     legend:  [{ color:'#rrggbb', label:'...' }]
 *     layers:  { name: THREE.Group }          added to the scene root
 *     explode: { name: [dx, dy, dz] }         offset applied when exploded
 *     ghosts:  [THREE.Material]               toggled by the transparency button
 *     camera:  { radius, target:[x,y,z] }
 *     extra:   optional { label, states:[string], apply(stateIndex) }
 *   }
 */
window.mekanika3d = (function () {
    'use strict';

    const THREE_URL = 'https://cdnjs.cloudflare.com/ajax/libs/three.js/r128/three.min.js';

    let threeLoading = null;
    let session = null;   // live viewer state, null when closed

    // ---------------------------------------------------------------- loading

    function loadThree() {
        if (window.THREE) return Promise.resolve(window.THREE);
        if (threeLoading) return threeLoading;

        threeLoading = new Promise((resolve, reject) => {
            const s = document.createElement('script');
            s.src = THREE_URL;
            s.async = true;
            s.onload = () => window.THREE ? resolve(window.THREE)
                                          : reject(new Error('three.js loaded but THREE is undefined'));
            s.onerror = () => {
                threeLoading = null;
                reject(new Error('Could not load the 3D library. Check your connection and try again.'));
            };
            document.head.appendChild(s);
        });
        return threeLoading;
    }

    // ------------------------------------------------------------ shared CSS

    function injectStyles() {
        if (document.getElementById('mek3d-styles')) return;
        const css = `
.mek3d-overlay{position:fixed;inset:0;z-index:9000;background:#1a1d23;color:#e8ebf0;
  font-family:Inter,Arial,Helvetica,sans-serif;overflow:hidden}
.mek3d-canvas{position:absolute;inset:0}
.mek3d-ui{position:absolute;top:12px;left:12px;z-index:10;display:flex;flex-direction:column;gap:8px}
.mek3d-btn{background:#2b303a;border:1px solid #3d4450;color:#e8ebf0;padding:8px 12px;border-radius:8px;
  font-size:13px;cursor:pointer;text-align:left;min-width:190px;font-family:inherit}
.mek3d-btn:hover{background:#353c48}
.mek3d-btn.on{background:#2E5496;border-color:#4472C4}
.mek3d-close{position:absolute;top:12px;right:12px;z-index:11;background:#2b303a;border:1px solid #3d4450;
  color:#e8ebf0;width:38px;height:38px;border-radius:8px;font-size:20px;line-height:1;cursor:pointer}
.mek3d-close:hover{background:#c0392b;border-color:#e74c3c}
.mek3d-legend{position:absolute;top:60px;right:12px;z-index:10;background:rgba(30,34,42,.85);
  border:1px solid #3d4450;border-radius:8px;padding:10px 12px;font-size:12px;line-height:1.65;max-width:260px}
.mek3d-legend b{display:block;margin-bottom:4px}
.mek3d-sw{display:inline-block;width:12px;height:12px;border-radius:3px;margin-right:6px;vertical-align:-1px}
.mek3d-measure{position:absolute;left:12px;bottom:36px;z-index:10;display:none;
  background:rgba(30,34,42,.92);border:1px solid #4472C4;border-radius:8px;padding:10px 12px;
  font-size:12px;line-height:1.6;min-width:200px}
.mek3d-measure b{display:block;margin-bottom:6px;color:#FFD34D}
.mek3d-measure .row{display:flex;justify-content:space-between;gap:18px}
.mek3d-measure .row span:last-child{font-family:Consolas,monospace;color:#fff}
.mek3d-hint{position:absolute;bottom:10px;left:12px;z-index:10;font-size:11px;color:#8b93a1}
.mek3d-title{position:absolute;bottom:10px;right:12px;z-index:10;font-size:11px;color:#8b93a1;
  text-align:right;max-width:60vw}
.mek3d-msg{position:absolute;inset:0;display:flex;align-items:center;justify-content:center;
  flex-direction:column;gap:14px;font-size:14px;color:#b9c0cc;z-index:20;text-align:center;padding:24px}
.mek3d-spin{width:28px;height:28px;border:3px solid #3d4450;border-top-color:#4472C4;border-radius:50%;
  animation:mek3dspin .8s linear infinite}
@keyframes mek3dspin{to{transform:rotate(360deg)}}
@media (max-width:640px){
  .mek3d-btn{min-width:0;padding:7px 10px;font-size:12px}
  .mek3d-legend{display:none}
  .mek3d-title{display:none}
}`;
        const el = document.createElement('style');
        el.id = 'mek3d-styles';
        el.textContent = css;
        document.head.appendChild(el);
    }

    // ------------------------------------------------------- geometry helpers

    function makeHelpers(THREE) {
        const H = {};

        H.mat = (hex, o) => new THREE.MeshStandardMaterial(
            Object.assign({ color: hex, metalness: 0.55, roughness: 0.45 }, o || {}));

        H.ghost = (hex, opacity) => H.mat(hex, {
            metalness: 0.25, roughness: 0.6,
            transparent: true, opacity: opacity === undefined ? 0.28 : opacity,
            side: THREE.DoubleSide, depthWrite: false
        });

        /* A full annulus as a Shape (ri = 0 gives a solid disc). */
        H.annulus = function (ri, ro) {
            const s = new THREE.Shape();
            s.absarc(0, 0, ro, 0, Math.PI * 2, false);
            if (ri > 1e-4) {
                const hole = new THREE.Path();
                hole.absarc(0, 0, ri, 0, Math.PI * 2, true);
                s.holes.push(hole);
            }
            return s;
        };

        /* An annular SECTOR from a0 to a1 - used for cutaway views. */
        H.annulusSector = function (ri, ro, a0, a1) {
            const s = new THREE.Shape();
            s.absarc(0, 0, ro, a0, a1, false);
            if (ri > 1e-4) {
                s.absarc(0, 0, ri, a1, a0, true);
            } else {
                s.lineTo(0, 0);
            }
            s.closePath();
            return s;
        };

        /*
         * A shaft cross-section carrying `n` keyways of width b and depth t.
         * The keyway is a true notch in the profile, not a decal.
         */
        H.shaftWithKeyways = function (r, b, t, n) {
            const s = new THREE.Shape();
            const half = Math.min(b / 2, r * 0.98);
            const alpha = 2 * Math.asin(half / r);      // angular width of the slot
            const rFloor = Math.max(r - t, 0.05);       // keyway floor radius
            const step = (Math.PI * 2) / n;

            let first = true;
            for (let i = 0; i < n; i++) {
                const c = i * step;                      // slot centre angle
                const a0 = c + alpha / 2;                // arc starts after this slot
                const a1 = c + step - alpha / 2;         // and ends before the next

                if (first) { s.moveTo(Math.cos(a0) * r, Math.sin(a0) * r); first = false; }
                s.absarc(0, 0, r, a0, a1, false);

                // Walk into the next slot: down the wall, across the floor, back out.
                // The arc already ended on the near wall of the next slot, so walk
                // down that wall, across the floor, and back out to the far wall.
                const nc = c + step;
                const w1 = { x: Math.cos(nc + alpha / 2) * r, y: Math.sin(nc + alpha / 2) * r };
                const dirX = Math.cos(nc), dirY = Math.sin(nc);
                const tanX = -dirY, tanY = dirX;
                s.lineTo(dirX * rFloor - tanX * half, dirY * rFloor - tanY * half);
                s.lineTo(dirX * rFloor + tanX * half, dirY * rFloor + tanY * half);
                s.lineTo(w1.x, w1.y);
            }
            s.closePath();
            return s;
        };

        /*
         * Extrude a Shape along the X axis (the shaft axis in every module here)
         * and centre it on the origin.
         */
        H.prism = function (shape, length, material, curveSegments, steps) {
            const g = new THREE.ExtrudeGeometry(shape, {
                depth: length, bevelEnabled: false,
                curveSegments: curveSegments || 64, steps: steps || 1
            });
            g.translate(0, 0, -length / 2);
            g.rotateY(Math.PI / 2);                    // extrusion axis Z -> X
            const m = new THREE.Mesh(g, material);
            m.castShadow = true; m.receiveShadow = true;
            return m;
        };

        H.box = function (sx, sy, sz, material) {
            const m = new THREE.Mesh(new THREE.BoxGeometry(sx, sy, sz), material);
            m.castShadow = true; m.receiveShadow = true;
            return m;
        };

        /*
         * Surface of revolution about the X axis, for parts whose radius varies
         * along the axis (tapers, shoulders, stepped shafts) - `prism` cannot do
         * these because it extrudes one constant cross-section.
         *
         * `profile` is the closed meridian as [[axial, radius], ...]. When the
         * sweep is partial the two open ends are capped with flat faces so the
         * cutaway reads as solid material rather than a hollow shell.
         *
         * Pass a DoubleSide material - the cut faces are single quads and would
         * otherwise vanish when viewed from behind.
         */
        H.lathe = function (profile, phiStart, phiLen, material, seg) {
            const grp = new THREE.Group();

            // LatheGeometry takes Vector2(radius, axial) and revolves about Y;
            // rotating -90 deg about Z lands the axis on X like every other part.
            const pts = profile.map(p => new THREE.Vector2(Math.max(p[1], 0), p[0]));
            pts.push(pts[0].clone());                       // close the meridian

            const g = new THREE.LatheGeometry(pts, seg || 72, phiStart, phiLen);
            g.rotateZ(-Math.PI / 2);
            const m = new THREE.Mesh(g, material);
            m.castShadow = true; m.receiveShadow = true;
            grp.add(m);

            if (phiLen < Math.PI * 2 - 1e-4) {
                const shape = new THREE.Shape();
                profile.forEach((p, i) => i === 0 ? shape.moveTo(p[0], p[1])
                                                  : shape.lineTo(p[0], p[1]));
                shape.closePath();
                // Local X stays axial; local Y must point along the radial
                // direction at angle a, which is (0, -sin a, cos a).
                [phiStart, phiStart + phiLen].forEach(a => {
                    const face = new THREE.Mesh(new THREE.ShapeGeometry(shape), material);
                    face.rotation.x = Math.PI / 2 + a;
                    grp.add(face);
                });
            }
            return grp;
        };

        /* Cylinder whose axis lies along X. */
        H.cylX = function (r, len, material, seg) {
            const m = new THREE.Mesh(new THREE.CylinderGeometry(r, r, len, seg || 48), material);
            m.rotation.z = Math.PI / 2;
            m.castShadow = true; m.receiveShadow = true;
            return m;
        };

        /*
         * Helical spring as a swept tube, axis along X.
         * Dead (ground) end coils are laid flat so closed-ground ends read correctly.
         */
        H.helix = function (meanRadius, wireR, length, totalCoils, deadCoils, material) {
            const pts = [];
            const N = Math.max(160, Math.round(totalCoils * 36));
            const active = Math.max(totalCoils - 2 * deadCoils, 0.1);

            for (let i = 0; i <= N; i++) {
                const c = totalCoils * (i / N);             // coil number along the wire
                let f;                                       // axial fraction 0..1
                if (c <= deadCoils) f = 0;
                else if (c >= totalCoils - deadCoils) f = 1;
                else f = (c - deadCoils) / active;

                const ang = 2 * Math.PI * c;
                pts.push(new THREE.Vector3(
                    -length / 2 + f * length,
                    Math.sin(ang) * meanRadius,
                    Math.cos(ang) * meanRadius));
            }
            const curve = new THREE.CatmullRomCurve3(pts);
            // 20 radial facets keep adjacent-face angle (18 deg) below the edge
            // threshold, so EdgesGeometry does not draw longitudinal seams along
            // the wire - the tube stays smooth and does not flood the snap list.
            const g = new THREE.TubeGeometry(curve, N, wireR, 20, false);
            const m = new THREE.Mesh(g, material);
            m.castShadow = true; m.receiveShadow = true;
            return m;
        };

        return H;
    }

    // -------------------------------------------------------------- builders

    const BUILDERS = {};

    /* ---------------------------------------------------- interference fit */
    BUILDERS['interference-fit'] = function (THREE, p, H) {
        const d = num(p.d, 50), L = num(p.L, 40);
        const di = num(p.di, 0), Da = Math.max(num(p.Da, d * 1.6), d * 1.05);

        const rs = d / 2, ri = di / 2, ra = Da / 2;
        const shaftLen = L * 2.4;

        const mShaft = H.mat(0x9EA3AB, { metalness: 0.85, roughness: 0.3 });
        const mHub = H.mat(0x4CB38C, { metalness: 0.35, roughness: 0.55 });
        const mFace = H.mat(0xE8674C, { metalness: 0.4, roughness: 0.5,
                                        emissive: 0x3a1109, emissiveIntensity: 0.5 });

        const gShaft = new THREE.Group(), gHub = new THREE.Group(), gFace = new THREE.Group();

        // Cut away the quarter that faces the default camera, so the joint is
        // open to the viewer the moment it opens. H.prism maps shape angle 0 to
        // world -Z, so keeping [pi, 2.5pi] removes the +Y/+Z quadrant.
        const A0 = Math.PI, A1 = Math.PI * 2.5;
        gShaft.add(H.prism(H.annulusSector(ri, rs, A0, A1), shaftLen, mShaft));
        gHub.add(H.prism(H.annulusSector(rs, ra, A0, A1), L, mHub));

        // The interference itself is microns - far too small to draw. Show the
        // contact surface as a highlighted band instead of faking a gap.
        //
        // CylinderGeometry sweeps its own theta about Y. rotateZ(pi/2) followed by
        // rotateY(pi) lands it on the X axis with the SAME angular convention that
        // H.prism uses, so the band's opening lines up with the cutaway. Rotating
        // the mesh by z=+pi/2 alone leaves it 90 degrees out, which put the
        // highlight over solid material and left the exposed interface bare.
        const faceGeom = new THREE.CylinderGeometry(rs * 1.004, rs * 1.004, L, 64, 1, true,
                                                    A0, A1 - A0);
        faceGeom.rotateZ(Math.PI / 2);
        faceGeom.rotateY(Math.PI);
        const faceMesh = new THREE.Mesh(faceGeom, mFace);
        faceMesh.userData.noEdges = true;   // highlight band, not a real part
        gFace.add(faceMesh);

        const pr = num(p.pressure, 0);
        return {
            title: `Ø${fmt(d)} × ${fmt(L)} mm joint · hub Ø${fmt(Da)}` +
                   (di > 0 ? ` · hollow shaft Ø${fmt(di)}` : ' · solid shaft') +
                   (pr > 0 ? ` · p = ${fmt(pr)} MPa` : ''),
            legend: [
                { color: '#9EA3AB', label: 'Shaft' },
                { color: '#4CB38C', label: 'Hub' },
                { color: '#E8674C', label: 'Contact interface (p = ' + fmt(pr) + ' MPa)' }
            ],
            layers: { shaft: gShaft, hub: gHub, face: gFace },
            explode: { hub: [0, 0, 0], shaft: [-L * 1.5, 0, 0] },
            ghosts: [mHub],
            camera: { radius: Math.max(Da * 3.2, 180), target: [0, 0, 0] }
        };
    };

    /* ------------------------------------------------------------ taper fit */
    BUILDERS['taper-fit'] = function (THREE, p, H) {
        const L = num(p.L, 40);
        const dm = num(p.dm, 50);
        const ratio = num(p.ratio, 10);

        // Fall back to deriving the end diameters from the taper ratio if the
        // engine did not supply them: taper = (D_large - D_small) / L = 1/ratio,
        // so each end sits L/(2*ratio) either side of the mean diameter.
        const dL = num(p.dLarge, dm + L / (2 * ratio));
        const dS = num(p.dSmall, dm - L / (2 * ratio));

        const rL = Math.max(dL, dS) / 2;
        const rS = Math.min(dL, dS) / 2;
        const ri = num(p.di, 0) / 2;
        const Ra = Math.max(num(p.Da, dm * 1.7) / 2, rL * 1.15);

        // Keep three quarters of the revolution; the missing quadrant is the one
        // facing the default camera, so the taper interface is visible.
        const PHI0 = 0, PHILEN = Math.PI * 1.5;

        const mShaft = H.mat(0x9EA3AB, { metalness: 0.85, roughness: 0.3,
                                         side: THREE.DoubleSide });
        const mHub = H.mat(0x4CB38C, { metalness: 0.35, roughness: 0.55,
                                       side: THREE.DoubleSide });
        const mFace = H.mat(0xE8674C, { metalness: 0.4, roughness: 0.5,
                                        side: THREE.DoubleSide,
                                        emissive: 0x3a1109, emissiveIntensity: 0.5 });

        // Shaft: cylindrical body, the tapered seat, then a smaller stub.
        // Large end at -X so the hub drives on from the small end (+X).
        const ext = L * 0.5;
        const x0 = -L / 2 - ext, x1 = -L / 2, x2 = L / 2, x3 = L / 2 + ext;
        const shaftProfile = [
            [x0, rL], [x1, rL], [x2, rS], [x3, rS], [x3, ri], [x0, ri]
        ];

        // Hub: bore follows the cone, outside is cylindrical.
        const hubProfile = [
            [x1, rL], [x2, rS], [x2, Ra], [x1, Ra]
        ];

        const gShaft = new THREE.Group(), gHub = new THREE.Group(), gFace = new THREE.Group();
        gShaft.add(H.lathe(shaftProfile, PHI0, PHILEN, mShaft));
        gHub.add(H.lathe(hubProfile, PHI0, PHILEN, mHub));

        // Contact interface. The interference is microns, so it is shown as a
        // highlighted conical band rather than a drawable gap.
        const faceGeom = new THREE.CylinderGeometry(rS * 1.004, rL * 1.004, L, 72, 1, true,
                                                    PHI0, PHILEN);
        faceGeom.rotateZ(-Math.PI / 2);
        const faceMesh = new THREE.Mesh(faceGeom, mFace);
        faceMesh.userData.noEdges = true;   // highlight band, not a real part
        gFace.add(faceMesh);

        const pr = num(p.pressure, 0);
        const halfAngle = num(p.halfAngle, 0);
        const disp = num(p.disp, 0);

        return {
            title: `Taper 1:${fmt(ratio)} · Ø${fmt(dS)} → Ø${fmt(dL)} over ${fmt(L)} mm` +
                   (halfAngle > 0 ? ` · half-angle ${halfAngle.toFixed(3)}°` : '') +
                   (disp > 0 ? ` · drive-up ${fmt(disp)} mm` : '') +
                   (pr > 0 ? ` · p = ${fmt(pr)} MPa` : ''),
            legend: [
                { color: '#9EA3AB', label: 'Shaft (taper 1:' + fmt(ratio) + ')' },
                { color: '#4CB38C', label: 'Hub (Ø' + fmt(Ra * 2) + ')' },
                { color: '#E8674C', label: 'Contact interface (p = ' + fmt(pr) + ' MPa)' }
            ],
            layers: { shaft: gShaft, hub: gHub, face: gFace },
            // The hub is driven on axially, so it comes off the same way.
            explode: { hub: [L * 1.3, 0, 0], face: [L * 1.3, 0, 0] },
            ghosts: [mHub],
            camera: { radius: Math.max(Ra * 3.4, L * 3, 180), target: [0, 0, 0] }
        };
    };

    /* ------------------------------------------------------- key connection */
    BUILDERS['key-connection'] = function (THREE, p, H) {
        const d = num(p.d, 40), b = num(p.b, 12), h = num(p.h, 8);
        const l = num(p.l, 50), t1 = num(p.t1, 5), t2 = num(p.t2, 3.3);
        const n = Math.max(1, Math.round(num(p.n, 1)));
        const Dh = num(p.Dhub, d * 1.9);

        const rs = d / 2, rh = Math.max(Dh / 2, rs * 1.25);
        const shaftLen = l * 2.2;

        const mShaft = H.mat(0x9EA3AB, { metalness: 0.85, roughness: 0.3 });
        const mHub = H.mat(0x4CB38C, { metalness: 0.35, roughness: 0.55 });
        const mKey = H.mat(0xD4AE4D, { metalness: 0.8, roughness: 0.35 });

        const gShaft = new THREE.Group(), gHub = new THREE.Group(), gKey = new THREE.Group();

        // Shaft carries real notches; hub bore carries the mating notches.
        gShaft.add(H.prism(H.shaftWithKeyways(rs, b, t1, n), shaftLen, mShaft));

        // Hub bore carries the mating relief. A hair of clearance keeps the bore
        // and the shaft OD from z-fighting where they coincide.
        const hubShape = H.annulus(0, rh);
        const bore = H.shaftWithKeyways(rs * 1.002, b, -t2, n);  // negative depth -> relief
        hubShape.holes.push(new THREE.Path(bore.getPoints(200)));
        gHub.add(H.prism(hubShape, l, mHub));

        // Keys are built in the SAME shape space as the shaft and hub profiles and
        // extruded through H.prism, so all three share one angular convention.
        // Placing the key as a plain Box meant re-deriving the shape->world mapping
        // by hand, which is what put it 90 degrees away from its keyway.
        const step = (Math.PI * 2) / n;
        for (let i = 0; i < n; i++) {
            const th = i * step;                        // same slot angle as the profiles
            const ux = Math.cos(th), uy = Math.sin(th); // radial unit vector
            const vx = -uy, vy = ux;                    // tangential unit vector
            const rIn = rs - t1, rOut = rs - t1 + h;    // key spans t1 below the surface
            const half = b / 2;

            const s = new THREE.Shape();
            s.moveTo(ux * rIn - vx * half, uy * rIn - vy * half);
            s.lineTo(ux * rOut - vx * half, uy * rOut - vy * half);
            s.lineTo(ux * rOut + vx * half, uy * rOut + vy * half);
            s.lineTo(ux * rIn + vx * half, uy * rIn + vy * half);
            s.closePath();
            gKey.add(H.prism(s, l, mKey, 1));
        }

        return {
            title: `Shaft Ø${fmt(d)} · key ${fmt(b)}×${fmt(h)}×${fmt(l)} mm · ` +
                   `t₁=${fmt(t1)} t₂=${fmt(t2)} · ${n} key${n > 1 ? 's' : ''} (DIN 6885)`,
            legend: [
                { color: '#9EA3AB', label: 'Shaft with keyway (t₁ = ' + fmt(t1) + ' mm)' },
                { color: '#D4AE4D', label: 'Parallel key ' + fmt(b) + '×' + fmt(h) },
                { color: '#4CB38C', label: 'Hub (representative Ø' + fmt(rh * 2) + ')' }
            ],
            layers: { shaft: gShaft, key: gKey, hub: gHub },
            explode: { hub: [0, d * 1.1, 0], key: [0, d * 0.5, 0], shaft: [0, 0, 0] },
            ghosts: [mHub],
            camera: { radius: Math.max(rh * 6.5, 200), target: [0, 0, 0] }
        };
    };

    /* ----------------------------------------------------- compression spring */
    BUILDERS['compression-spring'] = function (THREE, p, H) {
        const d = num(p.d, 3), Dm = num(p.Dm, 25), L0 = num(p.L0, 80);
        const nt = Math.max(num(p.nt, 8), 2.2), na = Math.max(num(p.na, nt - 2), 0.5);
        const Lc = num(p.Lc, nt * d);
        const L1 = num(p.L1, 0), L2 = num(p.L2, 0);

        const mWire = H.mat(0xB8C0CC, { metalness: 0.9, roughness: 0.25 });
        const mPlate = H.mat(0x3E8578, { metalness: 0.35, roughness: 0.6,
                                         transparent: true, opacity: 0.55,
                                         side: THREE.DoubleSide });

        const gSpring = new THREE.Group(), gPlates = new THREE.Group();
        const deadCoils = Math.max((nt - na) / 2, 0);
        const plateR = Dm * 0.75;

        // States the user can step through: free, then whatever the calculation used.
        const states = [{ label: 'Free length L₀ = ' + fmt(L0), len: L0 }];
        if (L1 > 0 && Math.abs(L1 - L0) > 0.01) states.push({ label: 'L₁ = ' + fmt(L1), len: L1 });
        if (L2 > 0 && Math.abs(L2 - L1) > 0.01) states.push({ label: 'L₂ = ' + fmt(L2), len: L2 });
        if (Lc > 0) states.push({ label: 'Solid length Lc = ' + fmt(Lc), len: Lc });

        let wire = null;
        function rebuild(len) {
            if (wire) { gSpring.remove(wire); wire.geometry.dispose(); }
            wire = H.helix(Dm / 2, d / 2, Math.max(len - d, d), nt, deadCoils, mWire);
            gSpring.add(wire);
            gPlates.children.forEach((pl, i) => { pl.position.x = (i === 0 ? -1 : 1) * len / 2; });
        }

        [-1, 1].forEach(s => {
            const pl = new THREE.Mesh(new THREE.CylinderGeometry(plateR, plateR, 3, 48), mPlate);
            pl.rotation.z = Math.PI / 2;
            pl.position.x = s * L0 / 2;
            gPlates.add(pl);
        });
        rebuild(L0);

        return {
            title: `d=${fmt(d)} · Dm=${fmt(Dm)} · L₀=${fmt(L0)} · ` +
                   `n=${fmt(na)} active / ${fmt(nt)} total (EN 13906-1)`,
            legend: [
                { color: '#B8C0CC', label: 'Wire Ø' + fmt(d) + ' mm' },
                { color: '#3E8578', label: 'Bearing plates' }
            ],
            layers: { spring: gSpring, plates: gPlates },
            explode: {},
            ghosts: [mPlate],
            camera: { radius: Math.max(L0 * 2.6, Dm * 4, 180), target: [0, 0, 0] },
            extra: {
                label: 'State',
                states: states.map(s => s.label),
                apply: i => rebuild(states[Math.min(i, states.length - 1)].len)
            }
        };
    };

    /* ----------------------------------------------------------- gear pair */
    BUILDERS['gear-pair'] = function (THREE, p, H) {
        /*
         * num() rejects anything <= 0 and substitutes its default, which is right for a
         * diameter and wrong for a profile shift or a helix angle - x = -0.2 would silently
         * become 0 and draw a different gear. Signed inputs go through snum instead.
         */
        const snum = (v, d) => {
            const n = typeof v === 'number' ? v : parseFloat(v);
            return isFinite(n) ? n : d;
        };

        const mn = num(p.mn, 2);
        const alphaN = num(p.alphaN, 20);
        const beta = snum(p.beta, 0);
        const z1 = Math.max(4, Math.round(num(p.z1, 20)));
        const z2 = Math.max(4, Math.round(num(p.z2, 40)));
        const x1 = snum(p.x1, 0), x2 = snum(p.x2, 0);
        const b1 = num(p.b1, 20), b2 = num(p.b2, 20);
        const a = num(p.a, mn * (z1 + z2) / 2);

        const d1 = num(p.d1, mn * z1 / Math.cos(beta * Math.PI / 180));
        const d2 = num(p.d2, mn * z2 / Math.cos(beta * Math.PI / 180));
        const da1 = num(p.da1, d1 + 2 * mn), da2 = num(p.da2, d2 + 2 * mn);
        const df1 = num(p.df1, d1 - 2.5 * mn), df2 = num(p.df2, d2 - 2.5 * mn);

        const bore1 = num(p.di1, 0), bore2 = num(p.di2, 0);
        const webbed1 = String(p.blank1 || '') === 'Webbed';
        const webbed2 = String(p.blank2 || '') === 'Webbed';
        const hub1 = num(p.hub1, 0), hub2 = num(p.hub2, 0);
        const rimIn1 = num(p.rim1, 0), rimIn2 = num(p.rim2, 0);
        const web1 = num(p.web1, 0), web2 = num(p.web2, 0);

        // Keep the pinion's metalness near the wheel's. At 0,85 it mirrored the dark scene and
        // read as a black disc next to the wheel - legible on a light background, not on this one.
        const mPinion = H.mat(0xB9C0C9, { metalness: 0.5, roughness: 0.42 });
        const mWheel = H.mat(0x4CB38C, { metalness: 0.5, roughness: 0.45 });
        const mHub = H.mat(0xD4AE4D, { metalness: 0.55, roughness: 0.4 });

        /*
         * One tooth flank is a true involute, sampled from the root to the tip.
         *
         *   half tooth angle at radius r:  psi(r) = s_t/d + inv(alpha_t) - inv(alpha_r)
         *
         * Below the base circle inv(alpha_r) is undefined and the generating rack leaves a
         * fillet, not an involute; psi is held at its base-circle value there, which draws a
         * radial flank down to the root. That is the standard schematic simplification and
         * the only place this outline departs from the real cut profile.
         */
        function gearShape(z, x, d, da, df, holeR) {
            const betaR = beta * Math.PI / 180, alphaNr = alphaN * Math.PI / 180;
            const alphaT = Math.atan(Math.tan(alphaNr) / Math.cos(betaR));
            const rb = d / 2 * Math.cos(alphaT);
            const ra = da / 2, rf = df / 2;

            const st = mn * (Math.PI / 2 + 2 * x * Math.tan(alphaNr)) / Math.cos(betaR);
            const psiRef = st / d;                       // half tooth angle at the reference circle
            const invT = Math.tan(alphaT) - alphaT;

            const psi = r => {
                if (r <= rb) return psiRef + invT;
                const ar = Math.acos(Math.min(1, rb / r));
                return psiRef + invT - (Math.tan(ar) - ar);
            };

            const N = 8;                                  // samples per flank
            const shape = new THREE.Shape();
            const step = Math.PI * 2 / z;
            const at = (r, ang) => [Math.cos(ang) * r, Math.sin(ang) * r];

            let started = false;
            const go = (r, ang) => {
                const pt = at(r, ang);
                if (!started) { shape.moveTo(pt[0], pt[1]); started = true; }
                else shape.lineTo(pt[0], pt[1]);
            };

            for (let i = 0; i < z; i++) {
                const c = i * step;

                // Rising flank, root -> tip.
                for (let k = 0; k <= N; k++) {
                    const r = rf + (ra - rf) * (k / N);
                    go(r, c - Math.max(psi(r), 0));
                }
                // Across the tip. A pointed tooth collapses to a single point rather than
                // crossing over itself.
                const psiA = Math.max(psi(ra), 0);
                if (psiA > 1e-6) go(ra, c + psiA);

                // Falling flank, tip -> root.
                for (let k = N; k >= 0; k--) {
                    const r = rf + (ra - rf) * (k / N);
                    go(r, c + Math.max(psi(r), 0));
                }
                // Root land across to the next tooth.
                go(rf, c + step - Math.max(psi(rf), 0));
            }
            shape.closePath();

            if (holeR > 1e-4) {
                const hole = new THREE.Path();
                hole.absarc(0, 0, holeR, 0, Math.PI * 2, true);
                shape.holes.push(hole);
            }
            return shape;
        }

        /*
         * A helical gear is the same transverse section rotated linearly along the axis:
         * the twist rate is tan(beta)/r_pitch. Applied to the extruded geometry rather than
         * faked, so the face advance the calculation reports is the one you can see.
         */
        function twistAboutX(geometry, radPerMm) {
            if (Math.abs(radPerMm) < 1e-12) return;
            const pos = geometry.attributes.position;
            for (let i = 0; i < pos.count; i++) {
                const px = pos.getX(i), py = pos.getY(i), pz = pos.getZ(i);
                const t = px * radPerMm, ct = Math.cos(t), stt = Math.sin(t);
                pos.setXYZ(i, px, py * ct - pz * stt, py * stt + pz * ct);
            }
            pos.needsUpdate = true;
            geometry.computeVertexNormals();
        }

        function buildGear(z, x, d, da, df, b, bore, webbed, hubD, rimInD, webT, mat, hand) {
            const g = new THREE.Group();
            const rBore = bore / 2;
            const twist = hand * Math.tan(beta * Math.PI / 180) / (d / 2);
            const steps = Math.abs(beta) > 0.01 ? Math.max(8, Math.round(b / 2)) : 1;

            const useWeb = webbed && rimInD > 0 && webT > 0 && webT < b;
            const rimInner = useWeb ? rimInD / 2 : rBore;

            // Toothed rim. Everything else hangs off it.
            const rim = H.prism(gearShape(z, x, d, da, df, rimInner), b, mat, 1, steps);
            twistAboutX(rim.geometry, twist);
            g.add(rim);

            if (useWeb) {
                const rHub = Math.max(hubD / 2, rBore * 1.15);
                // Web: a thin disc from the hub out to the rim bore, centred on the face.
                g.add(H.prism(H.annulus(rHub, rimInner + 0.01), webT, mat, 64));
                // Hub: full face width, so the blank has something to clamp on.
                g.add(H.prism(H.annulus(rBore, rHub), b, mHub, 64));
            }
            return g;
        }

        const gPinion = new THREE.Group(), gWheel = new THREE.Group();
        gPinion.add(buildGear(z1, x1, d1, da1, df1, b1, bore1, webbed1, hub1, rimIn1, web1, mPinion, +1));
        gWheel.add(buildGear(z2, x2, d2, da2, df2, b2, bore2, webbed2, hub2, rimIn2, web2, mWheel, -1));

        /*
         * Meshing phase. Put a pinion TOOTH centre on the line of centres and a wheel SPACE
         * centre facing it. A tooth is symmetric about its own centre, so its neighbouring
         * space centre sits exactly half a pitch away at every radius - which makes this
         * correct for a profile-shifted pair too, where the reference circles are not the
         * ones in contact.
         */
        gPinion.rotation.x = Math.PI / 2;
        gWheel.rotation.x = -Math.PI / 2 - Math.PI / z2;
        gWheel.position.y = a;

        const span = da1 / 2 + a + da2 / 2;
        const hel = Math.abs(beta) > 0.01 ? ` · β=${fmt(beta)}°` : ' · spur';

        const legend = [
            { color: '#9EA3AB', label: `Pinion z₁=${z1}, Ø${fmt(da1)} tip` },
            { color: '#4CB38C', label: `Wheel z₂=${z2}, Ø${fmt(da2)} tip` }
        ];
        if ((webbed1 && rimIn1 > 0) || (webbed2 && rimIn2 > 0)) {
            legend.push({ color: '#D4AE4D', label: 'Hub' });
        }
        legend.push({ color: '#6c757d', label: 'Root fillets are drawn as radial flanks below the base circle' });

        return {
            title: `mₙ=${fmt(mn)} · z=${z1}/${z2} · a=${fmt(a)} mm${hel} · ` +
                   `x=${fmt(x1)}/${fmt(x2)} (ISO 21771)`,
            legend: legend,
            layers: { pinion: gPinion, wheel: gWheel },
            explode: { wheel: [0, a * 0.55, 0] },
            ghosts: [mWheel],
            camera: { radius: Math.max(span * 1.5, 180), target: [0, a / 2, 0] }
        };
    };

    // ------------------------------------------------------------- utilities

    function num(v, dflt) {
        const n = typeof v === 'number' ? v : parseFloat(v);
        return (isFinite(n) && n > 0) ? n : dflt;
    }
    function fmt(v) {
        if (!isFinite(v)) return '-';
        return Math.abs(v) >= 100 ? v.toFixed(0) : (Math.abs(v) >= 10 ? v.toFixed(1) : v.toFixed(2));
    }

    // ------------------------------------------------------------- overlay UI

    function buildOverlay() {
        injectStyles();
        const ov = document.createElement('div');
        ov.className = 'mek3d-overlay';
        ov.innerHTML =
            '<div class="mek3d-canvas"></div>' +
            '<button class="mek3d-close" title="Close (Esc)">&times;</button>' +
            '<div class="mek3d-ui"></div>' +
            '<div class="mek3d-legend"></div>' +
            '<div class="mek3d-measure"></div>' +
            '<div class="mek3d-hint">Drag: rotate · Wheel: zoom · Right-drag: pan · ' +
            'Measure: snaps to corners &amp; edges · Esc: close</div>' +
            '<div class="mek3d-title"></div>' +
            '<div class="mek3d-msg"><div class="mek3d-spin"></div><div>Loading 3D view…</div></div>';
        document.body.appendChild(ov);
        document.body.style.overflow = 'hidden';
        return ov;
    }

    function showMessage(ov, html) {
        const m = ov.querySelector('.mek3d-msg');
        m.innerHTML = html;
        m.style.display = 'flex';
    }

    // ------------------------------------------------------------------ open

    function open(moduleKey, params) {
        if (session) close();

        const ov = buildOverlay();
        const esc = e => { if (e.key === 'Escape') close(); };
        document.addEventListener('keydown', esc);
        ov.querySelector('.mek3d-close').onclick = close;
        session = { ov, esc, raf: null, renderer: null, disposables: [] };

        if (!BUILDERS[moduleKey]) {
            showMessage(ov, '<div>No 3D view is defined for this module yet.</div>');
            return Promise.resolve();
        }

        return loadThree()
            .then(THREE => { if (session && session.ov === ov) start(THREE, ov, moduleKey, params || {}); })
            .catch(err => showMessage(ov, '<div>' + (err.message || 'Failed to load the 3D view.') + '</div>'));
    }

    /**
     * Case-insensitive view of the parameters a builder receives.
     *
     * IJSRuntime serialises with JsonSerializerDefaults.Web, which camelCases every
     * property name. So a Build3dParameters() key that starts with a capital never
     * arrives under the name the builder reads: `L` lands as `l`, `Da` as `da`,
     * `Dm`/`L0`/`Lc`/`L1`/`L2` as `dm`/`l0`/`lc`/`l1`/`l2`, `Dhub` as `dhub`. The
     * builder then read `undefined` and silently fell back to a hardcoded default,
     * so the viewer drew a plausible-looking model that was NOT the user's
     * calculation - worst in compression-spring, where nearly every dimension is
     * an uppercase key.
     *
     * Reading case-insensitively removes the trap for good, instead of relying on
     * everyone remembering to keep both sides lowercase. (Same serializer-defaults
     * trap as the jsonb one in the library rules - it has now bitten three times.)
     */
    function caseInsensitiveParams(params) {
        const map = {};
        Object.keys(params || {}).forEach(k => {
            const lk = k.toLowerCase();
            // Two keys differing only in case would be indistinguishable after
            // serialisation anyway - flag it rather than silently picking one.
            if (lk in map) console.warn('[mekanika3d] parameter case collision:', k);
            map[lk] = params[k];
        });
        return new Proxy(map, {
            get: (t, prop) => typeof prop === 'string' ? t[prop.toLowerCase()] : t[prop],
            has: (t, prop) => typeof prop === 'string' ? prop.toLowerCase() in t : prop in t
        });
    }

    function start(THREE, ov, moduleKey, params) {
        const H = makeHelpers(THREE);
        let spec;
        try {
            spec = BUILDERS[moduleKey](THREE, caseInsensitiveParams(params), H);
        } catch (e) {
            console.error('[mekanika3d] builder failed', e);
            showMessage(ov, '<div>The geometry could not be generated from these inputs.</div>');
            return;
        }
        ov.querySelector('.mek3d-msg').style.display = 'none';

        const host = ov.querySelector('.mek3d-canvas');
        const W = () => host.clientWidth, Hh = () => host.clientHeight;

        const scene = new THREE.Scene();
        scene.background = new THREE.Color(0x1a1d23);
        const camera = new THREE.PerspectiveCamera(45, W() / Hh(), 0.5, 20000);
        const renderer = new THREE.WebGLRenderer({ antialias: true });
        renderer.setSize(W(), Hh());
        renderer.setPixelRatio(Math.min(devicePixelRatio, 2));
        renderer.shadowMap.enabled = true;
        host.appendChild(renderer.domElement);
        session.renderer = renderer;

        scene.add(new THREE.AmbientLight(0xffffff, 0.52));
        scene.add(new THREE.HemisphereLight(0xbcd3ff, 0x2a2f38, 0.72));
        const key = new THREE.DirectionalLight(0xffffff, 0.9);
        key.position.set(240, 400, 280);
        scene.add(key);
        const fill = new THREE.DirectionalLight(0xffffff, 0.25);
        fill.position.set(-220, 160, -220);
        scene.add(fill);

        const root = new THREE.Group();
        scene.add(root);
        root.rotation.y = -0.5;

        const layers = spec.layers || {};
        Object.keys(layers).forEach(k => root.add(layers[k]));

        // Where each layer sits before any explode offset. Captured once, here, because the
        // explode animation runs every frame and would otherwise overwrite it - see applyExplode.
        const layerHome = {};
        Object.keys(layers).forEach(k => layerHome[k] = layers[k].position.clone());

        // ---- edge overlays (legibility + snap targets)
        //
        // Metallic faces blend into one another, so without outlines the user
        // cannot tell where an edge is - let alone click one. EdgesGeometry keeps
        // only edges whose adjoining faces meet above a threshold angle, so it
        // emits the REAL geometric edges (rims, keyway walls, cut faces) and skips
        // the smooth tessellation seams of a cylinder. Those same edges become the
        // snap targets for the measure tool.
        const EDGE_ANGLE = 24;
        const edgeMat = new THREE.LineBasicMaterial({ color: 0x0d1014,
                                                      transparent: true, opacity: 0.6 });
        let snapSegments = [];   // { mesh, a:Vector3(local), b:Vector3(local) }

        function buildEdges() {
            const stale = [];
            root.traverse(o => { if (o.userData && o.userData.isEdgeOverlay) stale.push(o); });
            stale.forEach(o => { if (o.parent) o.parent.remove(o); o.geometry.dispose(); });
            snapSegments = [];

            const meshes = [];
            root.traverse(o => {
                if (o.isMesh && !o.userData.isEdgeOverlay && !o.userData.noEdges) meshes.push(o);
            });
            meshes.forEach(m => {
                let eg;
                try { eg = new THREE.EdgesGeometry(m.geometry, EDGE_ANGLE); }
                catch (e) { return; }
                if (!eg.attributes.position || eg.attributes.position.count === 0) { eg.dispose(); return; }

                const seg = new THREE.LineSegments(eg, edgeMat);
                seg.userData.isEdgeOverlay = true;
                seg.renderOrder = 2;
                m.add(seg);   // child of the mesh, so it moves with explode/state changes

                const pos = eg.attributes.position;
                for (let i = 0; i < pos.count; i += 2) {
                    snapSegments.push({
                        mesh: m,
                        a: new THREE.Vector3(pos.getX(i), pos.getY(i), pos.getZ(i)),
                        b: new THREE.Vector3(pos.getX(i + 1), pos.getY(i + 1), pos.getZ(i + 1))
                    });
                }
            });
        }
        buildEdges();

        const cam = spec.camera || {};
        const tgt = cam.target || [0, 0, 0];
        const target = new THREE.Vector3(tgt[0], tgt[1], tgt[2]);

        /*
         * Frame what was actually built, instead of trusting the builder's hand-tuned radius.
         *
         * The perspective camera's 45 degrees is the VERTICAL field of view; the horizontal
         * half-angle is scaled by the aspect ratio. In a narrow pane (the side panel runs about
         * 0,64) the horizontal angle is far tighter, and a radius tuned on a wide screen puts
         * part of the model off the sides. The gear pair showed this plainly: the pinion sat
         * fully in frame while the wheel was 41 % past the right edge, so the viewer looked
         * like it was drawing a single gear.
         *
         * So: measure the real vertices, take the radius that encloses them about the
         * builder's target, and back off far enough for the TIGHTER of the two half-angles.
         * cam.radius stays as a floor, which keeps a small part from filling the screen.
         */
        let baseRadius = cam.radius || 300;
        (function fitCamera() {
            const v = new THREE.Vector3();
            let far2 = 0, found = false;

            root.updateWorldMatrix(true, true);
            root.traverse(node => {
                const pos = node.geometry && node.geometry.attributes && node.geometry.attributes.position;
                if (!pos || (node.userData && node.userData.isEdgeOverlay)) return;
                for (let i = 0; i < pos.count; i++) {
                    v.fromBufferAttribute(pos, i).applyMatrix4(node.matrixWorld);
                    const d2 = v.distanceToSquared(target);
                    if (d2 > far2) far2 = d2;
                    found = true;
                }
            });
            if (!found || far2 <= 0) return;

            const vFov = 45 * Math.PI / 180;
            const aspect = Math.max(W() / Hh(), 0.2);
            const hFov = 2 * Math.atan(Math.tan(vFov / 2) * aspect);
            const half = Math.min(vFov, hFov) / 2;

            const fit = Math.sqrt(far2) / Math.sin(half) * 1.06;   // 6 % breathing room
            baseRadius = Math.max(baseRadius, fit);
        })();

        const grid = new THREE.GridHelper(baseRadius * 4, 24, 0x3a4150, 0x2a2f38);
        grid.position.y = -baseRadius * 0.55;
        scene.add(grid);

        // ---- orbit
        let radius = baseRadius, theta = 0.95, phi = 1.15;
        const pan = new THREE.Vector3();
        function upd() {
            const st = Math.sin(phi) * radius, y = Math.cos(phi) * radius;
            camera.position.set(Math.sin(theta) * st + target.x + pan.x,
                                y + target.y + pan.y,
                                Math.cos(theta) * st + target.z + pan.z);
            camera.lookAt(target.x + pan.x, target.y + pan.y, target.z + pan.z);
        }
        upd();

        let drag = false, rmb = false, px = 0, py = 0;
        let downX = 0, downY = 0, travelled = 0;
        const el = renderer.domElement;
        const onDown = e => {
            drag = true; rmb = (e.button === 2);
            px = e.clientX; py = e.clientY;
            downX = e.clientX; downY = e.clientY; travelled = 0;
        };
        const onUp = e => {
            // A press that barely moved is a click, not an orbit drag - only then
            // does it place a measurement point.
            if (drag && measuring && travelled < 5 && e && e.button === 0) pickPoint(e);
            drag = false;
        };
        const onMove = e => {
            if (!drag) return;
            travelled = Math.max(travelled, Math.hypot(e.clientX - downX, e.clientY - downY));
            const dx = e.clientX - px, dy = e.clientY - py;
            px = e.clientX; py = e.clientY;
            if (rmb) {
                const ps = radius * 0.0016;
                const right = new THREE.Vector3(Math.cos(theta), 0, -Math.sin(theta));
                pan.addScaledVector(right, -dx * ps); pan.y += dy * ps;
            } else {
                theta -= dx * 0.006;
                phi = Math.max(0.15, Math.min(Math.PI - 0.15, phi - dy * 0.006));
            }
            upd();
        };
        const onWheel = e => {
            e.preventDefault();
            radius = Math.max(baseRadius * 0.15, Math.min(baseRadius * 6,
                     radius * (1 + Math.sign(e.deltaY) * 0.08)));
            upd();
        };
        const onCtx = e => e.preventDefault();
        // Hover snapping (measure mode only, and not while dragging the view).
        // Defined here but calls computeSnap/setHover, which are created in the
        // measurement block below - it only runs on real pointer events, by which
        // time everything is initialised.
        const onHover = e => {
            if (!measuring || drag) { if (measuring) setHover(null); return; }
            setHover(computeSnap(e.clientX, e.clientY));
        };
        el.addEventListener('mousedown', onDown);
        window.addEventListener('mouseup', onUp);
        window.addEventListener('mousemove', onMove);
        el.addEventListener('mousemove', onHover);
        el.addEventListener('wheel', onWheel, { passive: false });
        el.addEventListener('contextmenu', onCtx);

        let lastT = null;
        const onTS = e => { lastT = e.touches; };
        const onTM = e => {
            e.preventDefault();
            if (e.touches.length === 1 && lastT && lastT.length === 1) {
                theta -= (e.touches[0].clientX - lastT[0].clientX) * 0.006;
                phi = Math.max(0.15, Math.min(Math.PI - 0.15,
                      phi - (e.touches[0].clientY - lastT[0].clientY) * 0.006));
                upd();
            } else if (e.touches.length === 2 && lastT && lastT.length === 2) {
                const d0 = Math.hypot(lastT[0].clientX - lastT[1].clientX, lastT[0].clientY - lastT[1].clientY);
                const d1 = Math.hypot(e.touches[0].clientX - e.touches[1].clientX,
                                      e.touches[0].clientY - e.touches[1].clientY);
                if (d1 > 0) radius = Math.max(baseRadius * 0.15, Math.min(baseRadius * 6, radius * (d0 / d1)));
                upd();
            }
            lastT = e.touches;
        };
        el.addEventListener('touchstart', onTS, { passive: false });
        el.addEventListener('touchmove', onTM, { passive: false });

        // ---- measurement
        //
        // Model units are millimetres (the builders are fed engine values directly),
        // so raycast hit points give real dimensions with no scaling.
        //
        // Besides the straight point-to-point distance the panel breaks the result
        // into the axial component (X is the shaft axis in every module) and the
        // radial component, which is what you actually want when checking a
        // diameter, a keyway depth or a fit length.
        const measureGroup = new THREE.Group();
        scene.add(measureGroup);
        const raycaster = new THREE.Raycaster();
        const panel = ov.querySelector('.mek3d-measure');
        let measuring = false;
        let picks = [];

        // Marker that follows the cursor's snap point so the user sees exactly
        // which corner/edge will be picked before clicking.
        const hoverMarker = new THREE.Mesh(
            new THREE.SphereGeometry(baseRadius * 0.014, 16, 12),
            new THREE.MeshBasicMaterial({ color: 0x35E0C0, depthTest: false }));
        hoverMarker.renderOrder = 1000;
        hoverMarker.visible = false;
        scene.add(hoverMarker);

        const _proj = new THREE.Vector3();
        function toScreen(worldPt, rect) {
            _proj.copy(worldPt).project(camera);
            return { x: (_proj.x * 0.5 + 0.5) * rect.width,
                     y: (-_proj.y * 0.5 + 0.5) * rect.height,
                     behind: _proj.z > 1 };
        }
        function segDist2D(px, py, ax, ay, bx, by) {
            const dx = bx - ax, dy = by - ay;
            const len2 = dx * dx + dy * dy;
            let t = len2 > 0 ? ((px - ax) * dx + (py - ay) * dy) / len2 : 0;
            t = Math.max(0, Math.min(1, t));
            const cx = ax + t * dx, cy = ay + t * dy;
            return { dist: Math.hypot(px - cx, py - cy), t };
        }

        // Snap the cursor to the nearest real corner, then edge, then fall back to
        // the plain surface point. Corners win with a slightly wider radius so a
        // deliberate click near a vertex always lands exactly on it.
        const VERTEX_PX = 12, EDGE_PX = 9;
        const _wa = new THREE.Vector3(), _wb = new THREE.Vector3();
        function computeSnap(clientX, clientY) {
            const rect = el.getBoundingClientRect();
            const px = clientX - rect.left, py = clientY - rect.top;
            root.updateMatrixWorld(true);

            let best = null, bestD = Infinity, kind = null;

            // 1. corners (edge endpoints)
            for (const s of snapSegments) {
                _wa.copy(s.a).applyMatrix4(s.mesh.matrixWorld);
                const A = toScreen(_wa, rect);
                if (!A.behind) {
                    const d = Math.hypot(A.x - px, A.y - py);
                    if (d < VERTEX_PX && d < bestD) { bestD = d; best = _wa.clone(); kind = 'vertex'; }
                }
                _wb.copy(s.b).applyMatrix4(s.mesh.matrixWorld);
                const B = toScreen(_wb, rect);
                if (!B.behind) {
                    const d = Math.hypot(B.x - px, B.y - py);
                    if (d < VERTEX_PX && d < bestD) { bestD = d; best = _wb.clone(); kind = 'vertex'; }
                }
            }
            if (best) return { point: best, kind };

            // 2. nearest point along an edge
            bestD = Infinity;
            for (const s of snapSegments) {
                _wa.copy(s.a).applyMatrix4(s.mesh.matrixWorld);
                _wb.copy(s.b).applyMatrix4(s.mesh.matrixWorld);
                const A = toScreen(_wa, rect), B = toScreen(_wb, rect);
                if (A.behind || B.behind) continue;
                const r = segDist2D(px, py, A.x, A.y, B.x, B.y);
                if (r.dist < EDGE_PX && r.dist < bestD) {
                    bestD = r.dist; best = _wa.clone().lerp(_wb, r.t); kind = 'edge';
                }
            }
            if (best) return { point: best, kind };

            // 3. plain surface hit
            const ndc = new THREE.Vector2((px / rect.width) * 2 - 1, -(py / rect.height) * 2 + 1);
            raycaster.setFromCamera(ndc, camera);
            const hits = raycaster.intersectObject(root, true);
            for (const h of hits) {
                if (h.object.userData.isEdgeOverlay || h.object.userData.noEdges) continue;
                return { point: h.point.clone(), kind: 'surface' };
            }
            return null;
        }

        function setHover(snap) {
            if (!snap) { hoverMarker.visible = false; return; }
            hoverMarker.position.copy(snap.point);
            hoverMarker.material.color.setHex(
                snap.kind === 'vertex' ? 0x35E0C0 : snap.kind === 'edge' ? 0xFFD34D : 0x8b93a1);
            hoverMarker.visible = true;
        }

        function clearMeasure() {
            measureGroup.children.slice().forEach(c => {
                measureGroup.remove(c);
                if (c.geometry) c.geometry.dispose();
                if (c.material) c.material.dispose();
            });
            picks = [];
            hoverMarker.visible = false;
            drawPanel();
        }

        function drawPanel() {
            if (!measuring) { panel.style.display = 'none'; return; }
            panel.style.display = 'block';
            if (picks.length === 0) {
                panel.innerHTML = '<b>Measure</b><div>Hover a corner or edge — the marker ' +
                                  'shows the snap point — then click.</div>';
                return;
            }
            if (picks.length === 1) {
                panel.innerHTML = '<b>Measure</b><div>Click the second point.</div>';
                return;
            }
            const a = picks[0], c = picks[1];
            const dx = c.x - a.x, dy = c.y - a.y, dz = c.z - a.z;
            const row = (k, v) => '<div class="row"><span>' + k + '</span><span>' +
                                  v.toFixed(2) + ' mm</span></div>';
            panel.innerHTML = '<b>Measure</b>' +
                row('Distance', Math.hypot(dx, dy, dz)) +
                row('Axial (ΔX)', Math.abs(dx)) +
                row('Radial', Math.hypot(dy, dz)) +
                '<div style="margin-top:6px;color:#8b93a1">Click again for a new measurement.</div>';
        }

        function pickPoint(e) {
            const snap = computeSnap(e.clientX, e.clientY);
            if (!snap) return;

            if (picks.length >= 2) clearMeasure();
            const p = snap.point;
            picks.push(p);

            const dot = new THREE.Mesh(
                new THREE.SphereGeometry(baseRadius * 0.011, 16, 12),
                new THREE.MeshBasicMaterial({ color: 0xFFD34D, depthTest: false }));
            dot.position.copy(p);
            dot.renderOrder = 999;
            measureGroup.add(dot);

            if (picks.length === 2) {
                const line = new THREE.Line(
                    new THREE.BufferGeometry().setFromPoints(picks),
                    new THREE.LineBasicMaterial({ color: 0xFFD34D, depthTest: false }));
                line.renderOrder = 999;
                measureGroup.add(line);
            }
            drawPanel();
        }

        // ---- control buttons
        const ui = ov.querySelector('.mek3d-ui');
        const mkBtn = (label, on) => {
            const b = document.createElement('button');
            b.className = 'mek3d-btn' + (on ? ' on' : '');
            b.textContent = label;
            ui.appendChild(b);
            return b;
        };

        const ghosts = spec.ghosts || [];
        let transparent = true;
        if (ghosts.length) {
            const bT = mkBtn('Section view: transparent', true);
            const solidOpacity = 0.97;
            const baseOpacity = ghosts.map(m => m.opacity);
            bT.onclick = () => {
                transparent = !transparent;
                ghosts.forEach((m, i) => {
                    m.opacity = transparent ? baseOpacity[i] : solidOpacity;
                    m.transparent = true;
                    m.depthWrite = !transparent;
                    m.needsUpdate = true;
                });
                bT.textContent = 'Section view: ' + (transparent ? 'transparent' : 'solid');
                bT.classList.toggle('on', transparent);
            };
        }

        const bM = mkBtn('Measure: off', false);
        bM.onclick = () => {
            measuring = !measuring;
            clearMeasure();                       // also refreshes the panel
            el.style.cursor = measuring ? 'crosshair' : '';
            bM.textContent = 'Measure: ' + (measuring ? 'on' : 'off');
            bM.classList.toggle('on', measuring);
        };

        const explode = spec.explode || {};
        let exploded = false, expT = 0;
        if (Object.keys(explode).length) {
            const bE = mkBtn('Exploded view', false);
            bE.onclick = () => {
                exploded = !exploded;
                bE.classList.toggle('on', exploded);
                // Parts move when exploded, so any existing measurement would no
                // longer correspond to the assembled geometry.
                clearMeasure();
            };
        }

        if (spec.extra && spec.extra.states && spec.extra.states.length > 1) {
            let idx = 0;
            const bX = mkBtn(spec.extra.states[0], true);
            bX.onclick = () => {
                idx = (idx + 1) % spec.extra.states.length;
                spec.extra.apply(idx);
                bX.textContent = spec.extra.states[idx];
                buildEdges();     // meshes were rebuilt at a different length
                clearMeasure();
            };
        }

        let spin = false;
        const bS = mkBtn('Auto-rotate: off', false);
        bS.onclick = () => {
            spin = !spin;
            bS.textContent = 'Auto-rotate: ' + (spin ? 'on' : 'off');
            bS.classList.toggle('on', spin);
        };

        const bR = mkBtn('Reset view', false);
        bR.onclick = () => {
            radius = baseRadius; theta = 0.95; phi = 1.15;
            pan.set(0, 0, 0); root.rotation.y = -0.5; upd();
        };

        // ---- legend + title
        const leg = ov.querySelector('.mek3d-legend');
        if (spec.legend && spec.legend.length) {
            leg.innerHTML = '<b>Components</b>' + spec.legend.map(i =>
                '<span class="mek3d-sw" style="background:' + i.color + '"></span>' + i.label).join('<br>');
        } else {
            leg.style.display = 'none';
        }
        ov.querySelector('.mek3d-title').textContent = spec.title || '';

        // ---- loop
        /*
         * The explode offset is ADDED to wherever the builder put the layer.
         *
         * This used to be a bare position.set(), which silently destroyed any position a
         * builder had assigned - and it ran every frame, so there was no way to see it in the
         * code that set the position. The first four builders bake their offsets into the
         * geometry and never position a layer, so nothing showed. The gear pair positions the
         * wheel at the centre distance, and the explode loop pulled it back onto the pinion
         * every frame: the two gears rendered concentrically and it looked like the viewer was
         * drawing a single gear.
         */
        function applyExplode(f) {
            Object.keys(explode).forEach(k => {
                const g = layers[k], o = explode[k];
                if (!g || !o) return;
                const base = layerHome[k];
                g.position.set(base.x + o[0] * f, base.y + o[1] * f, base.z + o[2] * f);
            });
        }
        function frame() {
            session.raf = requestAnimationFrame(frame);
            expT += ((exploded ? 1 : 0) - expT) * 0.08;
            applyExplode(expT);
            if (spin) root.rotation.y += 0.004;
            renderer.render(scene, camera);
        }
        frame();

        const onResize = () => {
            camera.aspect = W() / Hh();
            camera.updateProjectionMatrix();
            renderer.setSize(W(), Hh());
        };
        window.addEventListener('resize', onResize);

        session.cleanup = function () {
            window.removeEventListener('resize', onResize);
            window.removeEventListener('mouseup', onUp);
            window.removeEventListener('mousemove', onMove);
            el.removeEventListener('mousemove', onHover);
            el.removeEventListener('mousedown', onDown);
            el.removeEventListener('wheel', onWheel);
            el.removeEventListener('contextmenu', onCtx);
            el.removeEventListener('touchstart', onTS);
            el.removeEventListener('touchmove', onTM);
            scene.traverse(o => {
                if (o.geometry) o.geometry.dispose();
                if (o.material) (Array.isArray(o.material) ? o.material : [o.material])
                    .forEach(m => m.dispose());
            });
            renderer.dispose();
        };
    }

    function close() {
        if (!session) return;
        if (session.raf) cancelAnimationFrame(session.raf);
        if (session.cleanup) { try { session.cleanup(); } catch (e) { console.warn(e); } }
        document.removeEventListener('keydown', session.esc);
        if (session.ov && session.ov.parentNode) session.ov.parentNode.removeChild(session.ov);
        document.body.style.overflow = '';
        session = null;
    }

    /*
     * Builds a module's geometry without opening the overlay and returns the bounding box of
     * every layer, in millimetres. A builder cannot be eyeballed from a terminal, so this is
     * how a new one gets checked against its inputs: the boxes must match the diameters and
     * face widths that went in. Needs three.js, so it lazy-loads exactly like open() does.
     */
    async function inspect(moduleKey, params) {
        const THREE = await loadThree();
        const build = BUILDERS[moduleKey];
        if (!build) throw new Error('no builder for ' + moduleKey);

        const spec = build(THREE, caseInsensitiveParams(params), makeHelpers(THREE));
        const out = { title: spec.title, layers: {} };

        /*
         * Box3.setFromObject is NOT usable here. It takes each mesh's LOCAL bounding box and
         * transforms its eight corners, so any rotated part reports the axis-aligned box of a
         * rotated box - larger than the part. The wheel, phased by -90 deg - 180/z, came out
         * 7% oversize and looked like a geometry bug. Walk the real vertices instead.
         */
        const v = new THREE.Vector3();
        Object.keys(spec.layers).forEach(name => {
            const root = spec.layers[name];
            root.updateWorldMatrix(true, true);

            const min = [Infinity, Infinity, Infinity];
            const max = [-Infinity, -Infinity, -Infinity];

            root.traverse(node => {
                const pos = node.geometry && node.geometry.attributes && node.geometry.attributes.position;
                if (!pos) return;
                for (let i = 0; i < pos.count; i++) {
                    v.fromBufferAttribute(pos, i).applyMatrix4(node.matrixWorld);
                    ['x', 'y', 'z'].forEach((axis, k) => {
                        if (v[axis] < min[k]) min[k] = v[axis];
                        if (v[axis] > max[k]) max[k] = v[axis];
                    });
                }
            });
            out.layers[name] = { min: min, max: max };
        });
        return out;
    }

    return { open, close, inspect, supports: k => !!BUILDERS[k] };
})();
