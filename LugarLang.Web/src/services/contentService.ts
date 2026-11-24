
import type { ContentBundle, ContentVersion } from "../types/content";

import type {
    RoutePlanRequest,
    RoutePlanResponse,
} from "../types/routing";

const API_BASE_URL = "https://lugarlang-api.onrender.com";

// Use the local API while developing the web app.
// Use the deployed Render API in production.
const ROUTING_API_BASE_URL = import.meta.env.DEV
    ? "http://localhost:5092"
    : API_BASE_URL;

export async function getContent(): Promise<ContentBundle> {
    const response = await fetch(`${API_BASE_URL}/api/content`);

    if (!response.ok) {
        throw new Error(
            `Failed to retrieve LugarLang content: HTTP ${response.status}`
        );
    }

    return response.json() as Promise<ContentBundle>;
}

export async function getContentVersion(): Promise<ContentVersion> {
    const response = await fetch(`${API_BASE_URL}/api/content/version`);

    if (!response.ok) {
        throw new Error(
            `Failed to retrieve LugarLang content version: HTTP ${response.status}`
        );
    }

    return response.json() as Promise<ContentVersion>;
}

export async function planRoutes(
    request: RoutePlanRequest
): Promise<RoutePlanResponse> {

    const response = await fetch(
        `${ROUTING_API_BASE_URL}/api/routes/plan`,
        {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
            },
            body: JSON.stringify(request),
        }
    );

    if (!response.ok) {
        const errorText = await response.text();

        throw new Error(
            `Route planning failed: HTTP ${response.status}. ${errorText}`
        );
    }

    return response.json() as Promise<RoutePlanResponse>;
}
