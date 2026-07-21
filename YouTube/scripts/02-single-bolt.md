# Video #2: Single Bolt Calculator Tutorial
## VDI 2230 | Free Engineering Tool

**Duration:** 6:30 - 7:00 minutes
**URL:** https://mekanika.org/single-bolt
**Standard:** VDI 2230
**Priority:** #2 (most used module — 16 uses in analytics)

---

## ElevenLabs Settings
- **Voice:** Adam or Josh
- **Stability:** 75%
- **Clarity:** 80%
- **Speed:** 0.95x

---

## Full Script

---

### [0:00 - 0:20] INTRODUCTION — Screen: Mekanika single bolt page

"Welcome to Mekanika.

Bolted connections are everywhere in mechanical engineering. And they fail more often than they should — usually because the bolt was selected by experience rather than calculation.

In this tutorial, I'll show you how to use the Single Bolt Calculator, based on VDI 2230, to properly size and verify any preloaded bolted joint.

This is the most rigorous bolt calculation standard in use today. Let me show you how to use it."

---

### [0:20 - 1:00] THEORY — Screen: Simple bolt diagram or title card

"VDI 2230 is the German engineering guideline for the systematic calculation of high-duty bolted joints.

Unlike basic bolt calculations, VDI 2230 accounts for:

Preload loss due to embedding — surfaces flatten slightly after initial tightening.
Load introduction factor — where the external force enters the joint changes how the bolt sees it.
Thermal effects — temperature differences between bolt and clamped parts affect residual preload.

The key concept is the force ratio. When an external force is applied to a bolted joint, part of it loads the bolt further, and part of it reduces the clamping force. The split depends on the stiffness of both the bolt and the clamped components.

Getting this right is the difference between a joint that loosens in service and one that holds for years."

---

### [1:00 - 5:00] CALCULATOR DEMO — Screen: Live calculator walkthrough

"Let's work through a real example.

We're designing the mounting flange for a gearbox cover. The cover is loaded by a combination of axial force and shear force from the gear reaction loads.

Go to mekanika.org and select Single Bolt Calculator.

The form is organized into four sections: Bolt Specification, Loading, Joint Geometry, and Tightening.

[SCREEN: Bolt Specification section]
Starting with Bolt Specification.

Bolt type: ISO 4014 — this is a standard hex head bolt with partial thread.
Bolt size: M12.
Property class: 8.8. This is the most common class for structural bolted joints.

Notice the calculator automatically fills in the relevant bolt data — yield strength 640 megapascals, tensile stress area 84.3 square millimeters, and the pitch for torque calculations.

[SCREEN: Loading section]
Under Loading:

Axial working load: 8 kilonewtons. This is the force trying to separate the joint.
Transverse working load: 4 kilonewtons. This is the shear force.
Load application factor: 0.5 — this assumes the force is applied mid-way through the clamped length, which is typical for general use.
Number of bolts in the group: I'll select 4.

Note that when you enter the group count, the calculator divides the total load by the number of bolts. Each bolt sees 2 kilonewtons axial and 1 kilonewton shear.

[SCREEN: Joint Geometry section]
Joint Geometry:

Clamped length: 40 millimeters — this is the total thickness of the parts being clamped.
Clamped material: steel.
Surface condition: lightly oiled — friction coefficient 0.12.

The grip coefficient is calculated automatically from these inputs.

[SCREEN: Tightening section]
Under Tightening:

Tightening method: torque wrench.
Tightening factor: 1.6 — this is the standard value for a calibrated torque wrench. It accounts for friction scatter.
Utilization factor: 90 percent. We'll load the bolt to 90% of its yield point during tightening.

Click Calculate.

[SCREEN: Results — Preload section]
The results begin with Preload.

Required minimum preload: 14.2 kilonewtons. This is the minimum clamping force needed to prevent joint separation under the applied axial load.

Assembly preload: 18.8 kilonewtons — at 90% yield utilization with our M12 Grade 8.8 bolt.

Tightening torque: 78 Newton-meters. Set your torque wrench to this value.

[SCREEN: Results — Force Analysis section]
Force Analysis.

Additional bolt load from external force: 1.8 kilonewtons. This is the portion of the 2 kilonewton axial load that increases bolt tension.

Maximum bolt load in service: 20.6 kilonewtons.

Residual clamping force after embedding: 16.4 kilonewtons. The joint does not open. This is the key check.

[SCREEN: Results — Safety Factors section]
Safety Factors.

Safety against bolt yielding: 2.48. Green — the bolt does not yield in service.
Safety against joint separation: 1.82. Green — the joint stays clamped.
Safety against shear failure: 3.21. Green — the friction is sufficient.
Overall bolt utilization: 87%. Within our 90% target.

All results are green. The design is valid.

[SCREEN: Auto-size feature]
Let me also show you the auto-sizing feature.

Click the gear icon next to Bolt Size. Enter your total axial load and the number of bolts, and the calculator recommends the minimum bolt size based on VDI 2230 Table A7.

This is useful when you're starting a design and don't yet know the bolt size."

---

### [5:00 - 6:00] RESULTS INTERPRETATION — Screen: Results overview

"Let me put these numbers in context.

The most critical result is the residual clamping force — 16.4 kilonewtons. If this falls below zero, the joint opens and you have a problem. Ours is positive and well above zero, so the joint stays tight.

The tightening torque of 78 Newton-meters is what you tell your assembly team. Too low and the joint loosens. Too high and you yield the bolt during assembly. This calculated value removes the guesswork.

Embedding loss is often forgotten in simple calculations. Surfaces in contact flatten slightly during the first load cycles. VDI 2230 accounts for this automatically. It reduces the effective preload — typically 5 to 10 percent — so we always have margin in our final results.

Download the PDF report for your design file. It shows all inputs, intermediate calculations, and final results in a clean format."

---

### [6:00 - 6:30] TIPS — Screen: Relevant input fields

"Practical tips for bolted joint design.

One: Grade 8.8 is the default choice for most structural bolts. Use Grade 10.9 only when you need higher preload in a smaller bolt. Never use Grade 12.9 in dynamic applications — brittle failure risk.

Two: Always use a calibrated torque wrench for bolts above M8. Hand-tightening varies by 40 to 50 percent. This uncertainty is too large for structural joints.

Three: If you have an existing design with bolt loosening problems, check your residual clamping force first. Embedding and thermal effects are the most common culprits.

Four: For joints with significant shear loads, check whether friction is sufficient before adding shear pins. The calculator gives you this check directly in the transverse safety factor.

Five: Use the PDF export for every design you send to manufacturing. It provides the documented basis for your bolt torque specification."

---

### [6:30 - 7:00] OUTRO — Screen: YouTube outro card

"The Single Bolt Calculator is free at mekanika.org — VDI 2230, complete calculation, no registration.

Related calculators:
Clamp Connection — for clamped shaft connections.
Parallel Key — for keyed shaft-hub connections.

Subscribe for more engineering calculator tutorials.

Questions? Contact us at contact@mekanika.org.

Thanks for watching."

---

## Screen Recording Notes

| Timestamp | Screen | Action |
|-----------|--------|--------|
| 0:00-0:20 | Single Bolt page | Scroll slowly |
| 0:20-1:00 | Title card / static | No movement |
| 1:00-1:30 | Bolt Specification section | Select bolt type, size, grade |
| 1:30-2:15 | Loading section | Enter axial, shear, load factor |
| 2:15-2:45 | Joint Geometry section | Enter clamped length, material |
| 2:45-3:00 | Tightening section | Enter method, factor, utilization |
| 3:00-3:15 | Click Calculate | Pause, then click |
| 3:15-3:45 | Results: Preload section | Scroll slowly |
| 3:45-4:15 | Results: Force Analysis | Highlight additional bolt load |
| 4:15-4:45 | Results: Safety Factors | All green — pan across |
| 4:45-5:00 | Auto-size demo | Click gear icon, enter load |
| 5:00-6:00 | Results overview | Scroll, highlight key values |
| 6:00-6:30 | Tips — show relevant fields | Highlight tightening torque |
| 6:30-7:00 | Outro card | Subscribe + related calculators |

---

## Demo Values

| Parameter | Value |
|-----------|-------|
| Bolt type | ISO 4014 |
| Bolt size | M12 |
| Property class | 8.8 |
| Axial working load | 8 kN (total) |
| Transverse working load | 4 kN (total) |
| Load application factor | 0.5 |
| Number of bolts | 4 |
| Clamped length | 40 mm |
| Clamped material | Steel |
| Surface condition | Lightly oiled (μ = 0.12) |
| Tightening method | Torque wrench |
| Tightening factor | 1.6 |
| Utilization factor | 90% |

---

## YouTube Upload Settings

**Title:**
```
Single Bolt Calculator Tutorial - VDI 2230 | Free Engineering Tool
```

**Description:**
```
Learn how to properly size and verify bolted joints using the free Single Bolt Calculator on Mekanika.org, based on VDI 2230.

🔗 Try it now: https://mekanika.org/single-bolt

⏱️ Timestamps:
0:00 Introduction
0:20 Theory - VDI 2230 explained
1:00 Calculator Demo - Step by step
5:00 Results Interpretation
6:00 Tips & Best Practices
6:30 Next Steps

📚 Standards Covered:
- VDI 2230 — Systematic calculation of high-duty bolted joints
- Preload, embedding loss, force ratio
- Tightening torque and safety factors

🔧 Related Calculators:
- Clamp Connection: https://mekanika.org/clamp-connection
- Parallel Key: https://mekanika.org/key-connection

📐 Features:
✓ Free — no registration required
✓ Based on VDI 2230 standard
✓ Auto-sizing from applied load
✓ PDF export for documentation

📧 contact@mekanika.org
🌐 https://mekanika.org

#MechanicalEngineering #BoltedJoint #VDI2230 #BoltCalculation #EngineeringCalculator #M12 #PreloadCalculation #FreeTools
```

**Tags:**
```
VDI 2230, bolt calculation, preloaded joint, bolted connection, mechanical engineering, bolt sizing, tightening torque, preload, engineering calculator, free tool, M12 bolt, grade 8.8, bolt design, joint separation, clamping force, online calculator, mekanika
```

**Thumbnail text:** "SINGLE BOLT | VDI 2230 Tutorial"
**Category:** Science & Technology
