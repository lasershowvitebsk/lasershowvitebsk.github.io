window.turnstileApiReady = window.turnstileApiReady || false;

window.turnstileWidgetId = window.turnstileWidgetId ?? null;
window.turnstileContainerId = window.turnstileContainerId || null;

window.turnstileRendering = window.turnstileRendering || false;
window.turnstileCallbackInProgress =
    window.turnstileCallbackInProgress || false;

window.turnstileSolved = window.turnstileSolved || false;

window.onloadTurnstileCallback = function () {
    window.turnstileApiReady = true;

    console.log('[Turnstile] API fully loaded and ready');
};

window.renderTurnstile = function (containerId, siteKey, dotnetHelper) {
    if (!containerId || !siteKey || !dotnetHelper) {
        console.error(
            '[Turnstile] containerId, siteKey or dotnetHelper is missing'
        );

        return;
    }

    if (
        window.turnstileContainerId === containerId &&
        window.turnstileWidgetId !== null
    ) {
        console.log(
            '[Turnstile] Existing widget found. Resetting it for a new token.'
        );

        window.resetTurnstile();
        return;
    }

    if (window.turnstileRendering) {
        console.log(
            '[Turnstile] Render already in progress'
        );

        return;
    }

    window.turnstileRendering = true;
    window.turnstileCallbackInProgress = false;
    window.turnstileSolved = false;
    window.turnstileContainerId = containerId;

    waitForTurnstileApiAndElement(
        containerId,
        function (turnstileApi, container) {
            try {
                window.turnstileWidgetId = turnstileApi.render(container, {
                    sitekey: siteKey,
                    theme: 'dark',
                    size: 'flexible',
                    action: 'chat',

                    retry: 'auto',
                    'retry-interval': 8000,

                    callback: function (token) {
                        if (
                            window.turnstileCallbackInProgress ||
                            window.turnstileSolved
                        ) {
                            console.warn(
                                '[Turnstile] Duplicate token callback ignored'
                            );

                            return;
                        }

                        window.turnstileSolved = true;
                        window.turnstileCallbackInProgress = true;

                        console.log(
                            '[Turnstile] Token received. Sending to Blazor.'
                        );

                        Promise.resolve(
                            dotnetHelper.invokeMethodAsync(
                                'OnTurnstileVerified',
                                token
                            )
                        ).catch(function (error) {
                            console.error(
                                '[Turnstile] OnTurnstileVerified failed:',
                                error
                            );

                            window.turnstileSolved = false;
                            window.turnstileCallbackInProgress = false;
                        });
                    },

                    'expired-callback': function () {

                        if (window.turnstileSolved) {
                            console.log(
                                '[Turnstile] Late expired callback ignored'
                            );

                            return;
                        }

                        window.turnstileCallbackInProgress = false;

                        console.warn('[Turnstile] Token expired');

                        Promise.resolve(
                            dotnetHelper.invokeMethodAsync(
                                'OnTurnstileExpired'
                            )
                        ).catch(function (error) {
                            console.error(
                                '[Turnstile] OnTurnstileExpired failed:',
                                error
                            );
                        });
                    },

                    'error-callback': function (errorCode) {

                        if (window.turnstileSolved) {
                            console.warn(
                                '[Turnstile] Late widget error ignored:',
                                errorCode || 'unknown'
                            );

                            return;
                        }

                        window.turnstileCallbackInProgress = false;

                        console.error(
                            '[Turnstile] Widget error:',
                            errorCode || 'unknown'
                        );

                        Promise.resolve(
                            dotnetHelper.invokeMethodAsync(
                                'OnTurnstileError',
                                errorCode || 'unknown'
                            )
                        ).catch(function (error) {
                            console.error(
                                '[Turnstile] OnTurnstileError failed:',
                                error
                            );
                        });
                    }
                });

                console.log(
                    '[Turnstile] Widget rendered:',
                    window.turnstileWidgetId
                );
            } catch (error) {
                console.error(
                    '[Turnstile] Widget render exception:',
                    error
                );

                window.turnstileWidgetId = null;
                window.turnstileContainerId = null;
                window.turnstileSolved = false;

                Promise.resolve(
                    dotnetHelper.invokeMethodAsync(
                        'OnTurnstileError',
                        'render-failed'
                    )
                ).catch(function (callbackError) {
                    console.error(
                        '[Turnstile] Cannot notify Blazor:',
                        callbackError
                    );
                });
            } finally {
                window.turnstileRendering = false;
            }
        },
        function () {
            window.turnstileRendering = false;

            Promise.resolve(
                dotnetHelper.invokeMethodAsync(
                    'OnTurnstileError',
                    'api-or-container-timeout'
                )
            ).catch(function (error) {
                console.error(
                    '[Turnstile] Timeout callback failed:',
                    error
                );
            });
        }
    );
};

window.resetTurnstile = function () {
    window.turnstileSolved = false;
    window.turnstileCallbackInProgress = false;

    if (window.turnstileWidgetId === null) {
        console.warn(
            '[Turnstile] Reset requested but widget does not exist'
        );

        return;
    }

    try {
        if (
            window.turnstile &&
            typeof window.turnstile.reset === 'function'
        ) {
            window.turnstile.reset(window.turnstileWidgetId);

            console.log(
                '[Turnstile] Widget reset:',
                window.turnstileWidgetId
            );
        }
    } catch (error) {
        console.warn(
            '[Turnstile] Reset failed:',
            error
        );
    }
};

window.removeTurnstile = function () {
    window.turnstileRendering = false;
    window.turnstileCallbackInProgress = false;
    window.turnstileSolved = false;

    if (window.turnstileWidgetId !== null) {
        try {
            if (
                window.turnstile &&
                typeof window.turnstile.remove === 'function'
            ) {
                window.turnstile.remove(window.turnstileWidgetId);

                console.log(
                    '[Turnstile] Widget removed:',
                    window.turnstileWidgetId
                );
            }
        } catch (error) {
            console.warn(
                '[Turnstile] Remove failed:',
                error
            );
        }
    }

    window.turnstileWidgetId = null;
    window.turnstileContainerId = null;
};

function waitForTurnstileApiAndElement(
    elementId,
    onReady,
    onTimeout,
    attempts
) {
    attempts = attempts || 0;

    const apiReady =
        window.turnstile &&
        typeof window.turnstile.render === 'function';

    const element = document.getElementById(elementId);

    if (apiReady && element) {
        onReady(window.turnstile, element);
        return;
    }

    const maxAttempts = 200;

    if (attempts >= maxAttempts) {
        console.error(
            `[Turnstile] Timeout. ` +
            `apiReady=${Boolean(apiReady)}, ` +
            `elementFound=${Boolean(element)}`
        );

        if (typeof onTimeout === 'function') {
            onTimeout();
        }

        return;
    }

    setTimeout(function () {
        waitForTurnstileApiAndElement(
            elementId,
            onReady,
            onTimeout,
            attempts + 1
        );
    }, 50);
}