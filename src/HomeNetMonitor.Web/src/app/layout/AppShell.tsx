import { NavLink, Outlet } from "react-router";

type NavigationItem = {
  label: string;
  path: string;
  end?: boolean;
};

const navigationItems: NavigationItem[] = [
    {
        label: "Overview",
        path: "/",
        end: true,
    },
    {
        label: "Devices",
        path: "/devices",
    },
    {
        label: "Activity",
        path: "/activity",
    },
    {
        label: "Topology",
        path: "/topology",
    },
];

function AppShell() {
    return (
        <div className="app-shell">
            <header className="app-header">
                <div>
                    <p className="app-eyebrow">HOME NETWORK</p>
                    <h1 className="app-brand">HomeNetMonitor</h1>
                </div>

                <div className="collector-status">
                    <span className="collector-status_dot" />
                    <span>Collector setup pending</span>
                </div>
            </header>

            <main className="app-content">
                <Outlet />
            </main>

            <nav className="bottom-navigation" aria-label="Primary navigation">
                {navigationItems.map((item) => (
                <NavLink
                    key={item.path}
                    to={item.path}
                    end={item.end}
                    className={({ isActive }) =>
                        isActive
                            ? "bottom-navigation__link bottom-navigation__link--active"
                            : "bottom-navigation__link"
                    }
                >
                    {item.label}
                </NavLink>
                ))}
            </nav>
        </div>
    );  
}
export default AppShell;