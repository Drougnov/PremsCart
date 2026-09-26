import { type CSSProperties } from "react";
import { HomeDiscovery } from "./Discovery";
import Icon from "./Icon";

function PosterArt({ kind }: { kind: string }) {
  if (kind === "audio")
    return (
      <svg viewBox="0 0 180 160">
        <path
          d="M39 105V77a51 51 0 0 1 102 0v28"
          fill="none"
          stroke="currentColor"
          strokeWidth="15"
        />
        <rect
          x="28"
          y="85"
          width="35"
          height="59"
          rx="16"
          fill="currentColor"
        />
        <rect
          x="117"
          y="85"
          width="35"
          height="59"
          rx="16"
          fill="currentColor"
        />
      </svg>
    );
  if (kind === "wear")
    return (
      <svg viewBox="0 0 180 160">
        <path
          d="m62 23-32 16-22 43 28 16 13-22-5 70h92l-5-70 13 22 28-16-22-43-32-16q-28 28-56 0Z"
          fill="currentColor"
        />
        <path
          d="M65 22q25 47 50 0M63 105h54v26H63z"
          stroke="var(--poster-bg)"
          strokeWidth="3"
          fill="none"
        />
        <path
          d="M80 52v26m20-26v26"
          stroke="var(--poster-bg)"
          strokeWidth="3"
        />
      </svg>
    );
  if (kind === "tech")
    return (
      <svg viewBox="0 0 180 160">
        <rect
          x="44"
          y="19"
          width="92"
          height="124"
          rx="14"
          fill="currentColor"
        />
        <rect
          x="57"
          y="33"
          width="66"
          height="31"
          rx="5"
          fill="var(--poster-bg)"
        />
        {[0, 1, 2].flatMap((y) =>
          [0, 1, 2, 3].map((x) => (
            <rect
              key={`${x}${y}`}
              x={57 + x * 18}
              y={78 + y * 19}
              width="11"
              height="11"
              rx="3"
              fill="var(--poster-bg)"
            />
          )),
        )}
      </svg>
    );
  if (kind === "build")
    return (
      <svg viewBox="0 0 180 160">
        <rect
          x="43"
          y="40"
          width="94"
          height="80"
          rx="12"
          fill="none"
          stroke="currentColor"
          strokeWidth="5"
        />
        <rect x="68" y="60" width="44" height="40" fill="currentColor" />
        {[55, 75, 95, 115].map((x) => (
          <path
            key={x}
            d={`M${x} 23v17m0 80v17M25 ${x - 4}h18m94 0h18`}
            stroke="currentColor"
            strokeWidth="5"
          />
        ))}
      </svg>
    );
  return (
    <svg viewBox="0 0 180 160">
      <path
        d="M32 29q31-12 58 6 27-18 58-6v104q-33-10-58 7-25-17-58-7V29Z"
        fill="currentColor"
      />
      <path
        d="M90 38v95M46 49l29 8m-29 13 29 8m-29 13 29 8m30-42 29-8m-29 29 29-8m-29 29 29-8"
        stroke="var(--poster-bg)"
        strokeWidth="3"
        fill="none"
      />
    </svg>
  );
}
const posters = [
  {
    kind: "build",
    title: "Small parts.\nBig ideas.",
    tag: "MAKE SOMETHING",
    label: "Explore electronics",
    search: "arduino",
    bg: "#ed7958",
    ink: "#432619",
  },
  {
    kind: "wear",
    title: "New semester.\nYour style.",
    tag: "CAMPUS EDIT",
    label: "Explore clothing",
    search: "hoodie",
    bg: "#cbc2ec",
    ink: "#3c335f",
  },
  {
    kind: "book",
    title: "One more\nchapter.",
    tag: "THE BOOK CLUB",
    label: "Explore books",
    search: "book",
    bg: "#f0ce65",
    ink: "#2e372f",
  },
  {
    kind: "audio",
    title: "Find your\nfrequency.",
    tag: "EVERYDAY ESSENTIALS",
    label: "Explore accessories",
    search: "headphones",
    bg: "#234f47",
    ink: "#d4eed5",
  },
  {
    kind: "tech",
    title: "A little\nhead start.",
    tag: "STUDY SMARTER",
    label: "Explore study tools",
    search: "calculator",
    bg: "#9dcced",
    ink: "#214566",
  },
  {
    kind: "book",
    title: "Fresh pages.\nFresh ideas.",
    tag: "PASS IT ON",
    label: "Explore giveaways",
    search: "",
    bg: "#e8a796",
    ink: "#5a3438",
  },
  {
    kind: "build",
    title: "Made by\nyour people.",
    tag: "STUDENT STORES",
    label: "Visit student stores",
    search: "",
    bg: "#cbd5a9",
    ink: "#394831",
  },
];
export default function Home({
  token,
  firstName,
  admin = false,
}: {
  token: string;
  firstName?: string;
  admin?: boolean;
}) {
  return (
    <div className="home-page editorial-home">
      <section className="editorial-hero" aria-labelledby="hero-title">
        <div className="hero-eyebrow">
          <span /> YOUR CAMPUS. YOUR MARKETPLACE.
        </div>
        <h1 id="hero-title">
          Buy, rent, sell,
          <br />
          or give things <em>away.</em>
        </h1>
        <p className="hero-intro">
          PremsCart is a marketplace for Premier University students.
          <br className="mobile-break" /> Find an item, message the student, and meet on campus.
        </p>
        <div className="card-fan" aria-label="Explore campus categories">
          {posters.map((p, i) => (
            <a
              key={p.title}
              className={`fan-card fan-card-${i}`}
              style={
                {
                  "--i": i - 3,
                  "--angle": `${(i - 3) * 7}deg`,
                  "--poster-bg": p.bg,
                  "--poster-ink": p.ink,
                  "--delay": `${i * -0.7}s`,
                } as CSSProperties
              }
              href={
                i === 5
                  ? "/giveaways"
                  : i === 6
                    ? "/stores"
                    : `/marketplace?search=${p.search}`
              }
              aria-label={p.label}
            >
              <div className="fan-card-inner">
                <small>{p.tag}</small>
                <h2>
                  {p.title.split("\n").map((line, n) => (
                    <span key={n}>
                      {line}
                      <br />
                    </span>
                  ))}
                </h2>
                <div className="poster-art">
                  <PosterArt kind={p.kind} />
                </div>
                <div className="poster-footer">
                  <span>PREMSCART</span>
                  <Icon name="arrow" />
                </div>
              </div>
            </a>
          ))}
        </div>
        <div className="hero-bottom">
          <div className="hero-actions">
            <a className="button button-primary" href="/marketplace">
              Browse Shop <Icon name="arrow" />
            </a>
            <a
              className="button button-secondary"
              href={admin ? "/admin" : token ? "/wanted" : "/register"}
            >
              {admin
                ? "Open admin workspace"
                : token
                  ? "See campus requests"
                  : "Join your campus"}{" "}
              <Icon name={admin ? "dashboard" : token ? "wanted" : "plus"} />
            </a>
          </div>
          <p className="hero-caption">
            {token && firstName ? `Welcome back, ${firstName}. ` : ""}Good
            things deserve a second chapter.
          </p>
        </div>
      </section>
      <div className="campus-strip">
        <span>
          <Icon name="shield" /> University email verified
        </span>
        <span>
          <Icon name="message" /> Talk directly to students
        </span>
        <span>
          <Icon name="package" /> Meet right on campus
        </span>
      </div>
      <HomeDiscovery token={token} />
      <section className="home-section process-section">
        <div className="process-intro">
          <span className="eyebrow">LESS SCROLLING. MORE CONNECTING.</span>
          <h2>
            From “I need that”
            <br />
            to “see you on campus.”
          </h2>
          <p>
            No complicated steps. Just students helping students find their next
            good thing.
          </p>
          <a className="inline-link" href="/marketplace">
            Find something you love <Icon name="arrow" />
          </a>
        </div>
        <div className="process-list">
          {(
            [
              [
                "01",
                "search",
                "Find your thing",
                "Open Shop and use the offer-type filter for items to buy, rent, or receive as giveaways. Open an item to see details and save it if you like it.",
              ],
              [
                "02",
                "message",
                "Make it yours",
                "Buy it, request a rental, or message the student. Price offers have their own page so they are easy to track.",
              ],
              [
                "03",
                "package",
                "Meet. Pick up. Pass it on.",
                "Choose Main gate, Canteen, or Library, pick a date and time, then complete the handoff. Return rentals when you are done.",
              ],
            ] as const
          ).map(([n, icon, title, description]) => (
            <article key={n}>
              <span className="process-number">{n}</span>
              <div>
                <h3>{title}</h3>
                <p>{description}</p>
              </div>
              <span className="process-icon">
                <Icon name={icon} />
              </span>
            </article>
          ))}
        </div>
      </section>
      <section className="home-section campus-panel">
        <div className="campus-copy">
          <span className="eyebrow">MADE FOR CAMPUS</span>
          <h2>
            A familiar place.
            <br />A better way
            <br />
            to <em>pickup.</em>
          </h2>
          <p>
            Your next textbook, a weekend project, a hoodie with a little
            history. Keep useful things moving through a community you’re part
            of.
          </p>
          <a
            className="button button-light"
            href={admin ? "/admin" : token ? "/dashboard" : "/register"}
          >
            {admin
              ? "Manage PremsCart"
              : token
                ? "Your campus dashboard"
                : "Find your place here"}{" "}
            <Icon name="arrow" />
          </a>
        </div>
        <div className="campus-promises">
          <article>
            <div className="promise-visual">
              <span className="promise-avatar">PUC</span>
              <span className="verified-seal">
                <Icon name="check" />
              </span>
            </div>
            <div>
              <small>01 / PEOPLE YOU CAN REACH</small>
              <h3>University email verified.</h3>
              <p>
                Members confirm access to their university mailbox before
                trading.
              </p>
            </div>
          </article>
          <article>
            <div className="promise-visual chat-mini">
              <span>Hello! Still available?</span>
              <span>Yes, see you at the Library.</span>
            </div>
            <div>
              <small>02 / EVERYONE ON THE SAME PAGE</small>
              <h3>A conversation away.</h3>
              <p>Chat, agree on a time, and get updates along the way.</p>
            </div>
          </article>
          <article>
            <div className="promise-visual reputation-mini">
              <Icon name="star" />
              <Icon name="star" />
              <Icon name="star" />
              <Icon name="star" />
              <Icon name="star" />
            </div>
            <div>
              <small>03 / TRUST, ONE HANDOFF AT A TIME</small>
              <h3>Real pickups. Real reviews.</h3>
              <p>
                Feedback comes from completed transactions, including returned
                rentals.
              </p>
            </div>
          </article>
        </div>
      </section>
    </div>
  );
}
