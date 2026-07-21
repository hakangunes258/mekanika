# Video #3: Taper Fit Calculator Tutorial
## DIN 7190 | Free Engineering Tool

**Duration:** 6:30 - 7:00 minutes
**URL:** https://mekanika.org/taper-fit
**Standard:** DIN 7190 (tapered connections)
**Priority:** #3 (second most used module — 11 uses in analytics)

---

## ElevenLabs Settings
- **Voice:** Adam or Josh
- **Stability:** 75%
- **Clarity:** 80%
- **Speed:** 0.95x

---

## Full Script

---

### [0:00 - 0:20] INTRODUCTION — Screen: Mekanika taper fit page

"Welcome to Mekanika.

In this tutorial, I'll show you how to use the Taper Fit Calculator to design tapered shaft-hub connections.

Taper fits are used across a wide range of machinery — from machine tool spindles to gear couplings to impeller connections.

They have one major advantage over cylindrical interference fits: they can be assembled and disassembled without a press, using only a nut or a hydraulic puller.

Let me show you how to calculate one correctly."

---

### [0:20 - 1:00] THEORY — Screen: Simple taper diagram or title card

"A taper fit works on the same principle as a cylindrical interference fit — elastic deformation creates contact pressure, and friction transmits the load.

The difference is the geometry. Instead of a cylindrical interface, we have a conical one. When the hub is pushed axially onto the shaft, the taper converts axial movement into radial interference.

The key parameter is the taper ratio. A ratio of 1 to 10 means the diameter changes by 1 millimeter for every 10 millimeters of length. Steeper tapers require less axial force for a given interference, but they're more prone to self-loosening under vibration.

Standard tapers — like Morse tapers and metric steep tapers — are defined in ISO standards. The calculator includes these as presets.

The axial displacement during assembly directly controls the interference — and therefore the contact pressure. This is the main design variable you'll adjust."

---

### [1:00 - 5:00] CALCULATOR DEMO — Screen: Live calculator walkthrough

"Let's design a taper fit for a fan impeller mounted on a 50 millimeter diameter shaft.

The impeller transmits 200 Newton-meters of torque. We want to use a standard metric taper that allows easy removal for maintenance.

Go to mekanika.org and select Taper Fit Calculator.

The form has four sections: Geometry, Taper Settings, Loading, and Materials.

[SCREEN: Geometry section]
Starting with Geometry.

Mean diameter: 50 millimeters. This is the diameter at the middle of the contact zone.
Fit length: 60 millimeters. This is the axial length of the tapered contact.
Shaft inner diameter: zero — solid shaft.
Hub outer diameter: 90 millimeters.

[SCREEN: Taper Settings section]
Now for the Taper Settings.

Click the Standard Taper dropdown. You'll see standard options including Metric 1:10, Morse tapers, and DIN 6 tapers.

I'll select Metric 1:10 — this is the most common standard taper for machine elements.

Notice the taper ratio field updates automatically to 10.

Surface roughness: shaft 3.2 micrometers, hub 3.2 micrometers. These are typical values for turned and ground surfaces.

For axial displacement: this is how far we push the hub onto the shaft during assembly. More displacement means more interference.

I'll start with 0.5 millimeters and see what contact pressure that gives us.

[SCREEN: Loading section]
Under Loading:

Static friction coefficient: 0.12 — for clean, dry steel surfaces.
Dynamic friction coefficient: 0.10 — slightly lower, used for slip safety calculation.
Applied torque: 200 Newton-meters.
Applied axial force: 500 Newtons — the fan creates some axial thrust.

[SCREEN: Materials section]
Materials:

Shaft material: C45 — yield strength 430 megapascals.
Hub material: C45 — same material, same yield strength. A symmetric connection.

Click Calculate.

[SCREEN: Results — Geometry section]
Let's look at the results.

Geometric Properties: Mean diameter 50 millimeters, taper angle 2.86 degrees — calculated from our 1:10 ratio. Contact area 9,425 square millimeters.

Effective interference: 0.025 millimeters. The axial displacement of 0.5 millimeters through a 1:10 taper gives us 0.025 millimeters of radial interference. This is the actual expansion of the hub bore.

[SCREEN: Results — Contact Pressure section]
Contact Pressure: 36.8 megapascals.

Roughness correction factor: 0.85. Surface roughness reduces effective interference because the peaks flatten slightly under load.

Corrected interference: 0.021 millimeters. This is the value actually used for contact pressure calculation.

[SCREEN: Results — Forces section]
Forces and Moments:

Transmissible torque: 349 Newton-meters. Our required torque is 200 Newton-meters.
Transmissible axial force: 13,950 Newtons. Our applied axial force is only 500 Newtons.

Both are well within capacity.

Assembly force: 2.8 kilonewtons. This is the force needed to push the hub 0.5 millimeters onto the shaft. Achievable with a simple mechanical puller or press.

Disassembly force: 3.4 kilonewtons. Slightly higher due to static friction.

[SCREEN: Results — Safety Factors section]
Safety Factors:

Safety against torque slip: 1.75. Exceeds our minimum of 1.5. Green.
Safety against axial slip: 27.9. Extremely high — the axial load is trivial compared to what the joint can hold.
Safety against shaft yielding: 11.7. Green.
Safety against hub yielding: 6.4. Green.

All results are safe. But let me check one thing — can we increase the axial displacement slightly for more margin?

[SCREEN: Change axial displacement to 0.7mm]
Let me change axial displacement to 0.7 millimeters and recalculate.

Contact pressure increases to 51.5 megapascals. Torque safety factor is now 2.45. We have significant extra margin, which is useful for dynamic loading with vibration.

Assembly force increases to 3.9 kilonewtons — still manageable.

I'll keep 0.7 millimeters as the final design value."

---

### [5:00 - 6:00] RESULTS INTERPRETATION — Screen: Results overview

"Let me highlight the most important outputs.

The transmissible torque of 489 Newton-meters — after recalculating with 0.7 mm displacement — is 2.45 times our required 200 Newton-meters. For a fan application with potential vibration and start-stop cycles, this margin is appropriate.

The assembly force of 3.9 kilonewtons is critical for maintenance planning. Your assembly team needs a tool capable of applying this force axially — typically a flanged nut or hydraulic puller. Document this in your assembly instructions.

The disassembly force of 4.7 kilonewtons is what's needed to remove the hub. This drives the puller specification.

Notice the roughness correction factor. If your surface finish degrades — through wear or corrosion — effective interference drops and the joint may slip. The calculator shows this clearly. This is why cleaning and inspecting taper surfaces before reassembly matters.

Export the PDF for your design file. It includes all the forces and assembly instructions in one document."

---

### [6:00 - 6:30] TIPS — Screen: Relevant fields

"Practical tips for taper fit design.

One: The 1:10 taper is self-locking — it won't loosen during operation. Steeper tapers like 1:5 are easier to remove but can work loose under vibration. Choose your taper based on maintenance requirements.

Two: Always specify the axial displacement on your drawing — not just the interference. The assembler can measure axial displacement directly with a depth gauge. Measuring interference requires precision gauging.

Three: Lubricate the taper surfaces lightly during assembly. This reduces assembly force by 20 to 30 percent and prevents galling. But remember — lubrication reduces the friction coefficient, so use the lubricated value in your calculation.

Four: Mark the hub position before disassembly with a scribe line. When reassembling, advance to the original position, then add your specified displacement. This ensures consistent preload.

Five: For applications with significant axial loads — like pump impellers — add a retaining nut or bolt in addition to the taper fit. The taper provides the torque connection; the nut prevents axial separation."

---

### [6:30 - 7:00] OUTRO — Screen: YouTube outro card

"The Taper Fit Calculator is free at mekanika.org — no registration required.

Related calculators:
Interference Fit — for cylindrical press-fit connections.
Parallel Key — if you need a keyed connection alongside the taper.

Subscribe for more engineering calculator tutorials.

Questions? Contact us at contact@mekanika.org.

Thanks for watching."

---

## Screen Recording Notes

| Timestamp | Screen | Action |
|-----------|--------|--------|
| 0:00-0:20 | Taper Fit page | Scroll slowly, show module |
| 0:20-1:00 | Title card / static | No movement |
| 1:00-1:30 | Geometry section | Enter mean diameter, fit length, hub OD |
| 1:30-2:15 | Taper Settings section | Select 1:10 from dropdown, enter roughness |
| 2:15-2:45 | Loading section | Enter friction, torque, axial force |
| 2:45-3:00 | Materials section | Select C45 for both |
| 3:00-3:10 | Click Calculate | Pause, click |
| 3:10-3:40 | Results: Geometry | Highlight effective interference |
| 3:40-4:10 | Results: Contact Pressure | Highlight roughness correction |
| 4:10-4:40 | Results: Forces | Highlight assembly and disassembly forces |
| 4:40-5:00 | Recalculate with 0.7 mm | Show improved safety factors |
| 5:00-6:00 | Results overview | Scroll, highlight key outputs |
| 6:00-6:30 | Tips — show relevant fields | Highlight axial displacement field |
| 6:30-7:00 | Outro card | Subscribe + related calculators |

---

## Demo Values

| Parameter | Value |
|-----------|-------|
| Mean diameter | 50 mm |
| Fit length | 60 mm |
| Shaft inner diameter | 0 mm (solid) |
| Hub outer diameter | 90 mm |
| Standard taper | Metric 1:10 |
| Axial displacement | 0.7 mm (final) |
| Shaft roughness Rz | 3.2 μm |
| Hub roughness Rz | 3.2 μm |
| Static friction coeff. | 0.12 |
| Dynamic friction coeff. | 0.10 |
| Applied torque | 200 Nm |
| Applied axial force | 500 N |
| Shaft material | C45 |
| Hub material | C45 |

---

## YouTube Upload Settings

**Title:**
```
Taper Fit Calculator Tutorial - DIN 7190 | Free Engineering Tool
```

**Description:**
```
Learn how to design tapered shaft-hub connections using the free Taper Fit Calculator on Mekanika.org, based on DIN 7190.

🔗 Try it now: https://mekanika.org/taper-fit

⏱️ Timestamps:
0:00 Introduction
0:20 Theory - How taper fits work
1:00 Calculator Demo - Step by step
5:00 Results Interpretation
6:00 Tips & Best Practices
6:30 Next Steps

📚 Standards Covered:
- DIN 7190 — Tapered interference fits
- Taper ratios and standard tapers (1:10, Morse, DIN 6)
- Surface roughness correction
- Assembly and disassembly forces

🔧 Related Calculators:
- Interference Fit: https://mekanika.org/interference-fit
- Parallel Key: https://mekanika.org/key-connection

📐 Features:
✓ Free — no registration required
✓ Standard taper presets (1:10, Morse, DIN 6)
✓ Roughness correction per DIN 7190
✓ Assembly and disassembly force calculation
✓ PDF export for documentation

📧 contact@mekanika.org
🌐 https://mekanika.org

#MechanicalEngineering #TaperFit #DIN7190 #ShaftDesign #ConicalFit #EngineeringCalculator #FreeTools #MorseTaper
```

**Tags:**
```
taper fit, conical fit, Morse taper, DIN 7190, shaft hub connection, tapered connection, mechanical engineering, shaft design, contact pressure, assembly force, engineering calculator, free tool, 1:10 taper, taper ratio, machine elements, online calculator, mekanika
```

**Thumbnail text:** "TAPER FIT | DIN 7190 Tutorial"
**Category:** Science & Technology
