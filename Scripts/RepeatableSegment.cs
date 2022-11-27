using UnityEngine;

namespace SegmentedParallax {
  public class RepeatableSegment : MonoBehaviour {

    private SpriteRenderer _spriteRenderer;

    private void Awake() {
      _spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public SpriteRenderer GetSpriteRenderer() {
      return _spriteRenderer;
    }

  }
}