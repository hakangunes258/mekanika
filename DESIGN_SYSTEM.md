# Mekanika Design System - Professional Icon System

## 🎨 Overview

Mekanika artık modern, profesyonel bir ikon sistemi kullanıyor. Emoji'ler yerine **Font Awesome 6** ve **custom SVG** ikonlar ile clean, minimal bir görünüm sağlıyoruz.

---

## 📦 Components

### ModuleIcon Component

**Lokasyon:** `Shared/ModuleIcon.razor`

Tüm modüller için merkezi ikon yönetimi sağlar.

#### Kullanım Örnekleri

```razor
<!-- Basit kullanım -->
<ModuleIcon Type="geometry" Size="medium" Style="gradient" />

<!-- Custom stil ile -->
<ModuleIcon Type="force" Size="large" Style="solid" CustomStyle="margin-right: 12px;" />

<!-- Buton içinde -->
<button class="btn btn-primary">
    <ModuleIcon Type="calculate" Size="small" Style="default" />
    Calculate
</button>
```

#### Parametreler

| Parametre | Değerler | Açıklama |
|-----------|----------|----------|
| **Type** | geometry, force, stress, safety, material, vb. | İkon tipi (aşağıda tam liste) |
| **Size** | small, medium, large, xlarge | İkon boyutu (24px - 64px) |
| **Style** | default, gradient, solid, outline | İkon stili |
| **CustomStyle** | CSS string | Ek inline stil |

---

## 🔧 Available Icon Types

### Geometry & Dimensions
- `geometry` - fa-ruler-combined
- `dimensions` - fa-arrows-left-right-to-line
- `diameter` - fa-circle-notch

### Forces & Loading
- `force` - fa-bolt (⚡ yerine)
- `torque` - fa-rotate
- `loading` - fa-weight-hanging
- `pressure` - fa-gauge-high

### Stresses
- `stress` - fa-burst (💪 yerine)
- `tension` - fa-arrows-up-down
- `compression` - fa-compress
- `shear` - fa-scissors

### Safety & Quality
- `safety` - fa-shield-halved (🛡️ yerine)
- `check` - fa-circle-check (✓ yerine)
- `warning` - fa-triangle-exclamation (⚠️ yerine)
- `error` - fa-circle-xmark (❌ yerine)

### Materials
- `material` - fa-cube (🧱 yerine)
- `steel` - fa-industry

### Module Types
- `interference-fit` - fa-circle-dot
- `taper-fit` - fa-angle-right
- `key-connection` - fa-key (🔑 yerine)
- `bolt` - fa-bolt (🔩 yerine)
- `bearing` - fa-ring (🔘 yerine)
- `spring` - fa-arrows-up-down-left-right (🌀 yerine)
- `gear` - fa-gear (⚙️ yerine)

### Actions
- `calculate` - fa-calculator (▶ yerine)
- `pdf` - fa-file-pdf (📄 yerine)
- `clear` - fa-broom (🧹 yerine)
- `back` - fa-arrow-left (← yerine)
- `new` - fa-plus-circle (🆕 yerine)

### Info
- `info` - fa-circle-info (ℹ️ yerine)
- `help` - fa-circle-question
- `design` - fa-compass-drafting
- `recommendation` - fa-lightbulb (💡 yerine)

---

## 🎯 Migration Guide

### Eski Format (Emoji)
```razor
<div class="card-header">
    <span>📏</span>
    <h2>Shaft & Key Dimensions</h2>
</div>
```

### Yeni Format (ModuleIcon)
```razor
<div class="card-header">
    <ModuleIcon Type="geometry" Size="medium" Style="solid" />
    <h2>Shaft & Key Dimensions</h2>
</div>
```

---

## 📋 Standard Usage Patterns

### 1. Page Headers
```razor
<div class="page-header">
    <h1>
        <ModuleIcon Type="key-connection" Size="large" Style="gradient" CustomStyle="margin-right: 12px;" />
        Parallel Key Calculator
    </h1>
    <p>Parallel key connection design according to DIN 6885</p>
</div>
```

### 2. Card Headers (Input Forms)
```razor
<div class="card-header">
    <ModuleIcon Type="geometry" Size="medium" Style="solid" />
    <h2>Shaft & Key Dimensions</h2>
</div>
```

### 3. Results Cards
```razor
<div class="card-header">
    <ModuleIcon Type="force" Size="medium" Style="solid" />
    <h2>Forces & Moments</h2>
</div>
```

### 4. Buttons
```razor
<button class="btn btn-success btn-lg" @onclick="Calculate">
    <ModuleIcon Type="calculate" Size="small" Style="default" CustomStyle="margin-right: 8px;" />
    Calculate
</button>
```

### 5. Alert Messages
```razor
<div class="alert alert-success">
    <ModuleIcon Type="check" Size="small" Style="default" />
    <strong>OK:</strong> Design meets safety requirements.
</div>
```

### 6. Module Cards (Index Page)
```razor
<a href="key-connection" class="module-card">
    <ModuleIcon Type="key-connection" Size="xlarge" Style="gradient" />
    <h3>Parallel Key</h3>
    <p>Parallel key connection design according to DIN 6885.</p>
    <span class="badge">DIN 6885</span>
</a>
```

---

## 🎨 Design Tokens

### Icon Sizes (CSS)
```css
.icon-sm   → 24px × 24px (font-size: 12px)
.icon-md   → 32px × 32px (font-size: 16px)
.icon-lg   → 48px × 48px (font-size: 24px)
.icon-xl   → 64px × 64px (font-size: 32px)
```

### Icon Styles (CSS)
```css
.icon-default  → Transparent background, gray text
.icon-gradient → Purple gradient background, white icon, shadow
.icon-solid    → Light gray background, border
.icon-outline  → Transparent, purple border
```

---

## 🚀 Implementation Checklist

Yeni bir modülü icon system'e migrate ederken:

- [ ] Import ModuleIcon component (otomatik via _Imports.razor)
- [ ] Page header'da emoji → ModuleIcon
- [ ] Card header'larda emoji → ModuleIcon
- [ ] Button'larda emoji → ModuleIcon
- [ ] Alert mesajlarında emoji → ModuleIcon
- [ ] Ana sayfada (Index.razor) modül kartını güncelle

---

## 📚 Best Practices

### ✅ DO
- Page header'da `gradient` style kullan (vurgu için)
- Card header'larda `solid` style kullan (subtle)
- Button'larda `default` style kullan (basit)
- Size'ı context'e göre seç (header=large, card=medium, button=small)

### ❌ DON'T
- Aynı sayfada çok fazla farklı style karıştırma
- Çok büyük ikonlar kullanma (xlarge sadece module cards için)
- Font Awesome class'larını direkt kullanma (ModuleIcon component kullan)

---

## 🔄 Future Extensions

### Custom SVG Icons
Özel modüller için custom SVG eklenebilir:

```csharp
// ModuleIcon.razor @code section
private string SvgPath => Type switch
{
    "custom-module" => "M12 2L2 7v10l10 5 10-5V7L12 2z",
    _ => ""
};
```

### New Icon Types
Yeni modüller eklendiğinde FontAwesomeClass mapping'e eklenir:

```csharp
private string FontAwesomeClass => Type switch
{
    // ... existing mappings
    "new-module" => "fa-solid fa-new-icon",
    _ => ""
};
```

---

## 📦 Dependencies

- **Font Awesome 6.5.1** (CDN via index.html)
- **modern-icons.css** (custom styles)
- **Inter font** (Google Fonts)

---

## 🎯 Design Philosophy

**Modern Minimal Principles:**
1. **Clean & Readable** - Sans emojis, professional icons
2. **Consistent** - Same icon sizes and styles throughout
3. **Accessible** - Clear visual hierarchy
4. **Scalable** - Easy to add new icons
5. **Professional** - Engineering tool aesthetic

---

## 📝 Notes

- Icon fallback: Her icon type için emoji fallback var (Font Awesome yüklenemezse)
- Print mode: Print edildiğinde ikonlar gizlenir
- Mobile responsive: Mobilde otomatik resize
- Performance: Font Awesome CDN cached, fast load

---

**Version:** 1.0
**Last Updated:** 2025-02-11
**Maintainer:** Mekanika Development Team
