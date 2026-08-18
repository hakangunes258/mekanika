/*
    GA4 event forwarding, called from Services/AnalyticsService.cs.
    Events go to a first-party Cloudflare Worker (analytics.mekanika.org) rather
    than straight to gtag, so ad blockers do not drop them.

    THIS FILE MUST KEEP ITS LINE BREAKS. It shipped as a single minified line with
    a `//` comment 40 bytes in, which commented out the remaining 99% of the file:
    sendGAEvent and getSessionId were never defined, AnalyticsService caught the
    interop error and "failed silently", and not one custom event was ever sent.
    (Plain GA4 page views kept working - those come from the gtag snippet in
    index.html, which is a separate thing.) Never minify this file, and use block
    comments only.
*/

const SESSION_TIMEOUT_MS = 30 * 60 * 1000; /* 30 minutes (GA4 default) */

function getOrCreateSessionId() {
    const stored = sessionStorage.getItem('ga_session_data');
    const now = Date.now();

    if (stored) {
        const data = JSON.parse(stored);
        const timeSinceLastActivity = now - data.lastActivityTime;
        if (timeSinceLastActivity < SESSION_TIMEOUT_MS) {
            data.lastActivityTime = now;
            sessionStorage.setItem('ga_session_data', JSON.stringify(data));
            return {
                sessionId: data.sessionId,
                sessionNumber: data.sessionNumber,
                sessionStartTime: data.sessionStartTime,
                isNewSession: false
            };
        }
    }

    const sessionData = {
        sessionId: `${Date.now()}_${Math.random().toString(36).substr(2, 9)}`,
        sessionNumber: stored ? JSON.parse(stored).sessionNumber + 1 : 1,
        sessionStartTime: now,
        lastActivityTime: now
    };
    sessionStorage.setItem('ga_session_data', JSON.stringify(sessionData));

    return {
        sessionId: sessionData.sessionId,
        sessionNumber: sessionData.sessionNumber,
        sessionStartTime: sessionData.sessionStartTime,
        isNewSession: true
    };
}

function getEngagementTime(sessionStartTime) {
    return Date.now() - sessionStartTime;
}

function collectUserProperties() {
    return {
        language: navigator.language || 'unknown',
        screen_resolution: `${screen.width}x${screen.height}`,
        viewport_size: `${window.innerWidth}x${window.innerHeight}`,
        user_agent: navigator.userAgent,
        platform: navigator.platform || 'unknown'
    };
}

const PROXY_URL = 'https://analytics.mekanika.org';

window.sendGAEvent = function (eventName, parameters) {
    console.log("[GA4 Proxy] === Event Tracking Start ===");
    console.log("[GA4 Proxy] Event:", eventName);
    console.log("[GA4 Proxy] Parameters:", parameters);

    let clientId = localStorage.getItem('ga_client_id');
    if (!clientId) {
        clientId = crypto.randomUUID();
        localStorage.setItem('ga_client_id', clientId);
        console.log("[GA4 Proxy] Generated new client ID:", clientId);
    }

    const session = getOrCreateSessionId();
    console.log("[GA4 Proxy] Session ID:", session.sessionId);
    console.log("[GA4 Proxy] Session Number:", session.sessionNumber);
    console.log("[GA4 Proxy] New Session:", session.isNewSession);

    const engagementTime = getEngagementTime(session.sessionStartTime);
    console.log("[GA4 Proxy] Engagement Time:", engagementTime, "ms");

    const userProps = collectUserProperties();

    const payload = {
        eventName: eventName,
        parameters: {
            ...(parameters || {}),
            session_id: session.sessionId,
            engagement_time_msec: engagementTime,
            session_number: session.sessionNumber,
            language: userProps.language,
            screen_resolution: userProps.screen_resolution,
            viewport_size: userProps.viewport_size
        },
        clientId: clientId,
        timestamp: new Date().toISOString()
    };

    if (session.isNewSession && eventName !== 'session_start') {
        console.log("[GA4 Proxy] 🆕 Sending session_start event (new session detected)");
        const sessionStartPayload = {
            eventName: 'session_start',
            parameters: {
                session_id: session.sessionId,
                engagement_time_msec: 0,
                session_number: session.sessionNumber,
                language: userProps.language,
                screen_resolution: userProps.screen_resolution,
                viewport_size: userProps.viewport_size
            },
            clientId: clientId,
            timestamp: new Date().toISOString()
        };

        fetch(PROXY_URL, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(sessionStartPayload),
            keepalive: true
        }).catch(err => console.error("[GA4 Proxy] session_start failed:", err));

        const storedSession = JSON.parse(sessionStorage.getItem('ga_session_data'));
        storedSession.isNewSession = false;
        sessionStorage.setItem('ga_session_data', JSON.stringify(storedSession));
    }

    console.log("[GA4 Proxy] Sending to proxy:", PROXY_URL);
    console.log("[GA4 Proxy] Full payload:", payload);

    fetch(PROXY_URL, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload),
        keepalive: true /* Ensures request completes even if page unloads */
    }).then(response => {
        if (!response.ok) {
            throw new Error(`HTTP ${response.status}`);
        }
        return response.json();
    }).then(data => {
        console.log("[GA4 Proxy] ✅ Event sent successfully:", data);
        return true;
    }).catch(error => {
        console.error("[GA4 Proxy] ❌ Event failed:", error);
        return false;
    });

    return true;
};

window.getSessionId = function () {
    const session = getOrCreateSessionId();
    return session.sessionId;
};

/*
    Developer aid: call window.checkProxyStatus() from the browser console to see
    whether the Worker is reachable and what the current session looks like.

    It used to run itself on every page load, one second in. That was invisible
    while the file was dead; reviving the file as-is would have put an OPTIONS
    request plus a block of console output in front of every visitor on every
    page, for diagnostics nobody is reading. It stays available, it just does not
    fire on its own.
*/
window.checkProxyStatus = function () {
    console.log("=== PROXY STATUS CHECK ===");
    console.log("Testing connection to:", PROXY_URL);

    fetch(PROXY_URL, {
        method: 'OPTIONS',
        headers: { 'Content-Type': 'application/json' }
    }).then(response => {
        console.log("✅ Proxy is reachable. Status:", response.status);
        console.log("CORS headers:", response.headers.get('Access-Control-Allow-Origin'));

        const session = getOrCreateSessionId();
        console.log("📊 Current Session ID:", session.sessionId);
        console.log("📊 Session Number:", session.sessionNumber);
        console.log("📊 Session Age:", Math.floor((Date.now() - session.sessionStartTime) / 1000), "seconds");
        return true;
    }).catch(error => {
        console.error("❌ Proxy not reachable:", error);
        console.error("Make sure Cloudflare Worker is deployed and CORS is configured");
        return false;
    });
};

console.log('Mekanika analytics helpers loaded');
