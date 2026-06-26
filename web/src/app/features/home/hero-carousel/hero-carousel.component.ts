import { Component, OnInit, OnDestroy, signal } from '@angular/core';

interface Slide {
  image: string;
  alt: string;
}

@Component({
  selector: 'app-hero-carousel',
  imports: [],
  templateUrl: './hero-carousel.component.html',
  styleUrl: './hero-carousel.component.css',
})
export class HeroCarousel implements OnInit, OnDestroy {
  readonly slides: Slide[] = [
    { image: 'assets/images/herocarousel/hero-banner-1.svg', alt: 'Fresh Fruits & Vegetables — Up to 40% off' },
    { image: 'assets/images/herocarousel/hero-banner-2.svg', alt: 'Daily Grocery Offers' },
    { image: 'assets/images/herocarousel/hero-banner-3.svg', alt: 'Snacks & Beverages Deals' },
    { image: 'assets/images/herocarousel/hero-banner-4.svg', alt: 'Electronics & Accessories' },
    { image: 'assets/images/herocarousel/hero-banner-5.svg', alt: 'Free Delivery on orders above ₹199' },
  ];

  readonly current = signal(0);
  private timer: ReturnType<typeof setInterval> | null = null;

  ngOnInit(): void { this.startAutoPlay(); }
  ngOnDestroy(): void { this.stopAutoPlay(); }

  prev(): void {
    this.current.update(i => (i - 1 + this.slides.length) % this.slides.length);
    this.restartAutoPlay();
  }

  next(): void {
    this.current.update(i => (i + 1) % this.slides.length);
    this.restartAutoPlay();
  }

  goTo(i: number): void {
    this.current.set(i);
    this.restartAutoPlay();
  }

  private startAutoPlay(): void {
    this.timer = setInterval(() => this.current.update(i => (i + 1) % this.slides.length), 4500);
  }

  private stopAutoPlay(): void {
    if (this.timer) { clearInterval(this.timer); this.timer = null; }
  }

  private restartAutoPlay(): void { this.stopAutoPlay(); this.startAutoPlay(); }
}
