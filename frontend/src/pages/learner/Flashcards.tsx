import { useState } from "react";
import { useSearchParams } from "react-router-dom";
import { RotateCw, ChevronLeft, ChevronRight, Crown } from "lucide-react";
import { useFlashcards } from "../../hooks/useApiData";
import { useAuth } from "../../context/AuthContext";
import { Card, SectionHeading, Badge } from "../../components/ui/Primitives";
import { Button } from "../../components/ui/Button";

export default function Flashcards() {
  const { user } = useAuth();
  const [params] = useSearchParams();
  const certId = params.get("cert") ?? undefined;
  const { data: cards, isLoading } = useFlashcards(certId);

  const [index, setIndex] = useState(0);
  const [flipped, setFlipped] = useState(false);

  if (!user?.isPremium) {
    return (
      <Card className="p-8 max-w-lg mx-auto text-center">
        <Crown size={28} className="text-brand-primary mx-auto mb-3" />
        <h1 className="text-lg font-bold text-text-primary mb-2">Flashcards are a Premium feature</h1>
        <p className="text-sm text-text-secondary mb-6">
          Upgrade to Premium to unlock unlimited flashcards across every certification, generated from the full
          question bank.
        </p>
        <Button>Upgrade to Premium</Button>
      </Card>
    );
  }

  if (isLoading || !cards) return <p className="text-sm text-text-secondary">Loading flashcards...</p>;
  if (cards.length === 0) return <p className="text-sm text-text-secondary">No flashcards available yet for this certification.</p>;

  const card = cards[index % cards.length];

  const next = () => { setFlipped(false); setIndex((i) => (i + 1) % cards.length); };
  const prev = () => { setFlipped(false); setIndex((i) => (i - 1 + cards.length) % cards.length); };

  return (
    <div className="space-y-6 max-w-2xl mx-auto">
      <SectionHeading title="Flashcards" description={`Card ${index + 1} of ${cards.length}`} />

      <button
        onClick={() => setFlipped((f) => !f)}
        className="w-full text-left"
        aria-label="Flip flashcard"
      >
        <Card className="p-8 min-h-[260px] flex flex-col justify-between hover:shadow-md transition-shadow">
          <Badge tone="brand">{card.topic}</Badge>
          <p className="text-lg font-medium text-text-primary my-6">
            {flipped ? card.back : card.front}
          </p>
          <span className="flex items-center gap-1.5 text-xs text-text-secondary self-end">
            <RotateCw size={14} /> {flipped ? "Showing answer — click to flip back" : "Click to reveal answer"}
          </span>
        </Card>
      </button>

      <div className="flex items-center justify-between">
        <Button variant="secondary" onClick={prev}>
          <ChevronLeft size={16} /> Previous
        </Button>
        <Button onClick={next}>
          Next <ChevronRight size={16} />
        </Button>
      </div>
    </div>
  );
}
