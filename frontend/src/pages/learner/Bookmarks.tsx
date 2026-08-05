import { Bookmark, X } from "lucide-react";
import { Link } from "react-router-dom";
import { useMyBookmarks, useToggleBookmark } from "../../hooks/useApiData";
import { Card, SectionHeading, Badge } from "../../components/ui/Primitives";

export default function Bookmarks() {
  const { data: bookmarks, isLoading } = useMyBookmarks();
  const toggleBookmark = useToggleBookmark();

  return (
    <div className="space-y-6">
      <SectionHeading title="Bookmarks" description="Questions you've saved for later review" />

      {isLoading && <p className="text-sm text-text-secondary">Loading bookmarks...</p>}

      <div className="space-y-4">
        {bookmarks?.map((q) => (
          <Card key={q.questionId} className="p-5">
            <div className="flex items-start justify-between mb-2">
              <div className="flex items-center gap-2">
                <Badge tone="brand">{q.topic}</Badge>
                <Badge>{q.difficulty}</Badge>
              </div>
              <button
                onClick={() => toggleBookmark.mutate(q.questionId)}
                className="h-8 w-8 flex items-center justify-center rounded-md text-brand-primary hover:bg-bg-alt"
                aria-label="Remove bookmark"
                title="Remove bookmark"
              >
                <X size={16} />
              </button>
            </div>
            <p className="text-sm font-medium text-text-primary">{q.prompt}</p>
            <Link
              to={`/practice?cert=${q.certificationId}`}
              className="inline-block mt-3 text-sm font-semibold text-brand-primary hover:underline"
            >
              Practice this topic
            </Link>
          </Card>
        ))}

        {!isLoading && bookmarks?.length === 0 && (
          <div className="text-center py-10">
            <Bookmark size={28} className="text-text-secondary mx-auto mb-3" />
            <p className="text-sm text-text-secondary">
              You haven't bookmarked any questions yet. Tap the bookmark icon during practice to save one here.
            </p>
          </div>
        )}
      </div>
    </div>
  );
}
