type PlaceholderPageProps = {
    title: string;
    description: string;
};

function PlaceholderPage({
    title,
    description,
}: PlaceholderPageProps){
    return (
        <section className="page">
            <div className="page-heading">
                <p className="page-heading__eyebrow">HOMENETMONITOR</p>
                <h2>{title}</h2>
                <p>{description}</p>
            </div>

            <div className="placeholder-card">
                <p>This module will be built in a later phase</p>
            </div>
        </section>
    )
}

export default PlaceholderPage;