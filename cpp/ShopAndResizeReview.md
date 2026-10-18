# Shop presentation and fullscreen loading — 6 September 2026

## Findings and changes

The old shop was a generic option list stretched into the full workspace. It had no sell transaction, no item detail view, and the same small assortment at every counter. Shops now share a dedicated, fixed-height trading layout with a scrolling shelf, buy/sell tabs, item artwork, merchant identity, quantities, unit/total prices, remaining-gold preview and explicit equipment comparisons. Compact layouts shrink the artwork before sacrificing stats. Each of the three merchants has a distinct catalogue. Selling supports partial or complete stacks, rejects worn gear, and uses a consistent 40% offer. Transactions validate quantity, affordability and shop hours before mutation; they use the existing inventory and save systems.

The terminal entered its alternate screen separately for character creation and gameplay. It also wrote the bottom-right cell without disabling automatic wrapping, and could use different viewport measurements for a world frame and its overlay. These are concrete lifecycle/rendering defects consistent with the cropped HUD and resize-dependent town display. A single RAII terminal session now spans startup through play, disables edge wrapping, restores terminal settings on exit, caches valid viewport measurements during resize, freezes geometry for each composed frame and resets/redraws on size changes. The implementation follows Microsoft's [VT sequence and console-mode guidance](https://learn.microsoft.com/en-us/windows/console/console-virtual-terminal-sequences).

## Verified

- Optimized static Windows x64 build and complete native regression suite: pass. New tests cover distinct stock, purchase/sale pricing, bulk/repeated purchases, stacking, partial/full sales, acquisition metadata preservation during sale, worn-item protection, invalid quantities, insufficient funds, shop hours, no trade turns, persistence, empty sell views and frame geometry/reset contracts.
- 28 actual-executable shop captures across four views and seven viewport sizes: pass. Controls, prices, checkout and all four gear comparison rows remain visible without shared-panel clipping. Representative pictures were visually inspected in `qa-shops/`.
- 48 existing presentation captures across twelve screens and four sizes: pass.
- Two native Windows ConPTY scenarios: pass. One starts at 220×85; the other starts at 80×25 and expands during character creation. Both enter town, shrink to 80×25, expand to 220×85, buy and sell through the real shop input loop, and exit cleanly. Native console-cell readback confirms the first-row HUD, player glyph and last-row controls after each resize. A 32-gold sword bought and sold leaves 180 of 200 gold with zero turns elapsed. Original console mode is restored.
- Both existing saves load successfully; their checksums are unchanged by validation. No running player game was stopped.
- Both installed release copies are identical: `cpp/LumenRogue-CPP.exe` and `bin/cpp/LumenRogue-CPP.exe`, 1,623,552 bytes, SHA-256 `51FCD2CA91269A518DCBC73D9CB23D431622269ED3206591E43E6CB5D20CCD45`.

## Limits

Native resize testing uses Windows pseudoconsole resize events, not direct automation of the user's Windows Terminal fullscreen shortcut. All game visuals remain coloured terminal characters. No buyback ledger or merchant economy persistence is introduced: merchants have unlimited catalogue stock and buy at the documented fixed offer. The existing player saves and quest state are preserved.
