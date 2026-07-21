# Video #1: Interference Fit Calculator Tutorial
## DIN 7190 | Free Engineering Tool

**Duration:** 6:30 - 7:00 minutes
**URL:** https://mekanika.org/interference-fit
**Standard:** DIN 7190:2017
**Priority:** #1 (complex, high professional value)

---

## ElevenLabs Settings
- **Voice:** Adam or Josh
- **Stability:** 75%
- **Clarity:** 80%
- **Speed:** 0.95x

---

## Full Script

---

### [0:00 - 0:20] INTRODUCTION — Screen: Mekanika interference fit page

"Welcome to Mekanika.

In this tutorial, I'll show you how to use the Interference Fit Calculator to design press-fit and shrink-fit connections according to DIN 7190.

By the end of this video, you'll be able to calculate contact pressure, assembly force, and safety factors for any cylindrical interference joint — in under a minute."

---

### [0:20 - 1:00] THEORY — Screen: Simple diagram or static slide

"An interference fit creates a force-locked connection between a shaft and hub through elastic deformation.

The shaft is made slightly larger than the hub bore. When assembled — either by pressing or by thermal expansion — the two parts grip each other through surface friction.

DIN 7190 is the German standard that defines the calculation method for cylindrical interference joints.

The three key things we need to check are:

First — contact pressure. The joint must grip firmly enough to transmit your required torque.

Second — stress in the components. The pressure must not cause yielding in the shaft or hub.

Third — assembly feasibility. Can we actually press or heat-fit these parts?"

---

### [1:00 - 5:00] CALCULATOR DEMO — Screen: Live calculator walkthrough

"Let me walk through a real design example.

We have a gear hub that needs to be fixed onto a solid steel shaft. The shaft transmits 500 Newton-meters of torque, and we want a reliable connection without any additional fasteners.

Open your browser and go to mekanika.org. Click on Interference Fit Calculator.

You'll see the input form organized into four sections: Geometry, Interference, Materials, and Loading.

[SCREEN: Geometry section]
Let's start with Geometry.

Shaft outer diameter: 50 millimeters.
Shaft inner diameter: zero — this is a solid shaft.
Hub outer diameter: 100 millimeters.
Fit length: 80 millimeters.

[SCREEN: Interference section]
Now for the Interference.

Interference is the difference between the shaft and hub diameters. A larger interference gives more contact pressure — more grip — but also more stress.

I'll enter a minimum interference of 0.040 millimeters and a maximum of 0.060 millimeters.

The calculator will use the minimum interference for safety analysis and the maximum for stress analysis. This is the correct DIN 7190 approach.

[SCREEN: Materials section]
For Materials:

Shaft material: C45. This is a common structural steel with a yield strength of 430 megapascals.

Hub material: GG-25. This is grey cast iron. Notice it has lower yield strength — 160 megapascals. The hub is the limiting component.

[SCREEN: Loading section]
Under Loading:

Applied torque: 500 Newton-meters.
Safety factor requirement: I'll leave it at 1.5 — this is standard for normal operating conditions.
Friction coefficient: 0.12 — appropriate for a dry steel-to-cast-iron interface.

Everything is ready. Click Calculate.

[SCREEN: Results section]
The results are organized into five sections. Let me go through them.

Geometric Properties: The calculator confirms our inputs and shows the interference ratio — 0.001, which is within the typical range for this diameter.

Contact Pressure: Using the minimum interference of 0.040 millimeters, the contact pressure is 38.4 megapascals. This is the actual gripping force per unit area.

Stress Analysis: Shaft stress is 57.6 megapascals — well below the C45 yield strength of 430. Hub hoop stress is 115 megapascals — below GG-25 yield strength of 160. Both components are safe.

Safety Factors: Safety against slip — 1.87. This exceeds our required 1.5. Safety against shaft yielding — 7.5. Safety against hub yielding — 1.39.

Wait — hub yielding shows 1.39, which is below our 1.5 target. The row is highlighted in yellow.

This means the maximum interference of 0.060 is too large for our cast iron hub. The calculator is telling us to reduce the upper tolerance.

Let me change maximum interference to 0.050 millimeters and recalculate.

[SCREEN: Recalculate with 0.050]
Now hub yielding safety factor is 1.61 — safe.

Assembly: Press-fit force is 89 kilonewtons. You'll need a hydraulic press for this. Alternatively, for shrink fitting, heat the hub by 165 degrees Celsius for easy slip-on assembly."

---

### [5:00 - 6:00] RESULTS INTERPRETATION — Screen: Results table with highlights

"Let me summarize what these results mean for your design.

The joint will reliably transmit 500 Newton-meters with a safety factor of 1.87 — nearly double the minimum required. The connection is secure.

Both shaft and hub stresses are within material limits. No yielding will occur during assembly or operation.

The press force of 89 kilonewtons requires hydraulic equipment. If you only have a mechanical press rated to 50 kilonewtons, you have two options: reduce the interference further, or switch to shrink fitting by heating the hub.

The PDF export button at the top generates a complete calculation report. This is useful for quality documentation, customer submissions, or design reviews.

Notice the color coding in the safety factor table. Green rows are safe. Yellow means marginal — review your design. Red means the design is unsafe and must be changed before proceeding."

---

### [6:00 - 6:30] TIPS — Screen: Calculator with tips highlighted

"A few practical tips for interference fit design.

One: Always calculate with both minimum and maximum interference. Minimum gives you slip safety; maximum gives you stress safety. The calculator handles both automatically.

Two: Cast iron hubs have much lower yield strength than steel. They are often the limiting factor — as we just saw.

Three: For shrink fitting, ensure uniform, slow heating to avoid thermal gradients that cause internal stress.

Four: Add a small chamfer on the shaft end before press fitting. This reduces the peak force at the start of the operation and prevents galling.

Five: The friction coefficient depends heavily on surface condition. Lubricated assembly uses 0.10. Dry steel-to-steel uses 0.12 to 0.15. Always match the coefficient to your actual assembly process."

---

### [6:30 - 7:00] OUTRO — Screen: YouTube outro card

"The Interference Fit Calculator is available now at mekanika.org — completely free, no registration required.

Related calculators you might need next:
Taper Fit — for tapered interference connections.
Parallel Key — for keyed shaft-hub connections.

Subscribe to Mekanika for more engineering calculator tutorials.

If you have questions, leave a comment below or email contact@mekanika.org.

Thanks for watching."

---

## Screen Recording Notes

| Timestamp | Screen | Action |
|-----------|--------|--------|
| 0:00-0:20 | Interference Fit page | Scroll slowly, highlight module title |
| 0:20-1:00 | Static image or title card | No screen movement |
| 1:00-1:20 | Geometry input section | Type values slowly |
| 1:20-1:45 | Interference input section | Enter min/max values |
| 1:45-2:15 | Materials dropdowns | Select C45 and GG-25 |
| 2:15-2:30 | Loading section | Enter torque, friction |
| 2:30-2:45 | Click Calculate | Pause on button, then click |
| 2:45-3:30 | Results: Geometry + Contact Pressure | Scroll slowly |
| 3:30-4:00 | Results: Stress Analysis | Hover over values |
| 4:00-4:30 | Results: Safety Factors (yellow row) | Zoom in on yellow row |
| 4:30-5:00 | Recalculate with 0.050, new results | Show green safety factors |
| 5:00-6:00 | Results overview | Scroll through all sections |
| 6:00-6:30 | Tips — re-show relevant input fields | Highlight friction coefficient |
| 6:30-7:00 | YouTube outro card | Subscribe + related calculators |

---

## Demo Values

| Parameter | Value |
|-----------|-------|
| Shaft outer diameter | 50 mm |
| Shaft inner diameter | 0 mm (solid) |
| Hub outer diameter | 100 mm |
| Fit length | 80 mm |
| Min interference | 0.040 mm |
| Max interference | 0.050 mm |
| Shaft material | C45 |
| Hub material | GG-25 |
| Torque | 500 Nm |
| Safety factor req. | 1.5 |
| Friction coefficient | 0.12 |

---

## YouTube Upload Settings

**Title:**
```
Interference Fit Calculator Tutorial - DIN 7190 | Free Engineering Tool
```

**Description:**
```
Learn how to design press-fit and shrink-fit connections using the free Interference Fit Calculator on Mekanika.org, based on DIN 7190.

🔗 Try it now: https://mekanika.org/interference-fit

⏱️ Timestamps:
0:00 Introduction
0:20 Theory - What is an interference fit?
1:00 Calculator Demo - Step by step
5:00 Results Interpretation
6:00 Tips & Best Practices
6:30 Next Steps

📚 Standards Covered:
- DIN 7190:2017 — Cylindrical interference fits
- Contact pressure equations
- Safety factor guidelines

🔧 Related Calculators:
- Taper Fit: https://mekanika.org/taper-fit
- Parallel Key: https://mekanika.org/key-connection

📐 Features:
✓ Free — no registration required
✓ Based on DIN 7190 standard
✓ PDF export for documentation
✓ Color-coded safety factors

📧 contact@mekanika.org
🌐 https://mekanika.org

#MechanicalEngineering #EngineeringCalculator #InterferenceFit #DIN7190 #PressFit #ShrinkFit #ShaftDesign #FreeTools
```

**Tags:**
```
interference fit, press fit, shrink fit, DIN 7190, mechanical engineering, shaft design, hub connection, contact pressure, engineering calculator, free tool, mechanical calculations, shaft hub, assembly force, engineering design, online calculator, mechanical design, force fit, cylindrical joint, engineering tutorial, mekanika
```

**Thumbnail text:** "INTERFERENCE FIT | DIN 7190 Tutorial"
**Category:** Science & Technology
