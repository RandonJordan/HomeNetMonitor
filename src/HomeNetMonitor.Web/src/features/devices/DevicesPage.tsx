import { useEffect, useState } from "react";
import DeviceHistory from "./DeviceHistory";

type SavedDevice = {
    id: number;
    ipAddress: string | null;
    macAddress: string | null;
    hostname: string | null;
    displayName: string | null;
    model: string | null;
    vendor: string | null;
    firstSeenUtc: string;
    lastSeenUtc: string;
};

function DevicesPage() {
    const [devices, setDevices] = useState<SavedDevice[]>([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);
    const [selectedDeviceId, setSelectedDeviceId] = useState<number | null>(null);

    useEffect(() => {
        const controller = new AbortController();

        async function loadDevices() {
            try {
                const response = await fetch(
                    `${import.meta.env.VITE_API_BASE_URL}/api/network/devices/saved`,
                    { signal: controller.signal }
                );

                if (!response.ok) {
                    throw new Error(`API returned ${response.status}`);
                }

                const data: SavedDevice[] = await response.json();

                setDevices(data);
            } catch (err) {
                if (controller.signal.aborted) {
                    return;
                }

                setError(
                    err instanceof Error
                        ? err.message
                        : "Unable to load devices."
                );
            } finally {
                if (!controller.signal.aborted) {
                    setLoading(false);
                }
            }
        }

        loadDevices();

        return () => controller.abort();
    }, []);

    return (
        <section className="page">
            <div className="page-heading">
                <p className="page-heading__eyebrow">NETWORK INVENTORY</p>

                <h2>Devices</h2>

                <p>
                    Explore devices discovered on your home network
                    and review their last known information.
                </p>
            </div>

            <div className="devices-summary">
                <span>Known Devices</span>
                <strong>{devices.length}</strong>
            </div>

            {loading && (
                <p className="devices-message">Loading saved devices...</p>
            )}

            {error && (
                <p className="devices-message devices-message--error">
                    Unable to load devices: {error}
                </p>
            )}

            {!loading && !error && devices.length === 0 && (
                <p className="devices-message">
                    No saved devices found. Run a network scan to discover devices.
                </p>
            )}

            {!loading && !error && devices.length > 0 && (
                <div className="devices-list">
                    {devices.map((device) => (
                        <article className="device-card" key={device.id}>
                            <div className="device-card__header">
                                <h3>
                                    {device.displayName ||
                                        device.hostname ||
                                        device.vendor ||
                                        "Unknown Device"}
                                </h3>

                                <span className="device-card__id">
                                    #{device.id}
                                </span>
                            </div>

                            <p className="device-card__ip">
                                {device.ipAddress ?? "Unknown IP"}
                            </p>

                            <div className="device-card__details">
                                <span>
                                    Vendor: {device.vendor ?? "Unknown"}
                                </span>

                                <span>
                                    MAC: {device.macAddress ?? "Unavailable"}
                                </span>

                                <span>
                                    Last Seen:{" "}
                                    {new Date(
                                        device.lastSeenUtc + "Z"
                                    ).toLocaleString()}
                                </span>
                            </div>
                            <button
                                type="button"
                                className="device-card__history-button"
                                aria-expanded={selectedDeviceId === device.id}
                                onClick={() =>
                                    setSelectedDeviceId(
                                        selectedDeviceId === device.id ? null : device.id
                                    )
                                }
                            >
                                {selectedDeviceId === device.id
                                    ? "Hide History"
                                    : "View History"}
                            </button>

                            {selectedDeviceId === device.id && (
                                <DeviceHistory deviceId={device.id} />
                            )}
                        </article>
                    ))}
                </div>
            )}
        </section>
    );
}

export default DevicesPage;